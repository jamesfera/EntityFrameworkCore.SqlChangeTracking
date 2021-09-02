using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkCore.SqlChangeTracking.Logging;
using EntityFrameworkCore.SqlChangeTracking.Models;
using EntityFrameworkCore.SqlChangeTracking.SyncEngine.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine
{
    

    public delegate ValueTask<IChangeTrackingEntry<TEntity>[]> GetNextBatchDelegate<TEntity>(DbContext dbContext, string syncContext);
    
    public class DataSetState<TEntity> where TEntity : class, new()
    {
        object _previousPageToken = null;

        public async ValueTask<IChangeTrackingEntry<TEntity>[]> T1(DbContext dbContext, string syncContext)
        {
            var result = await dbContext.NextDataSetHelper<TEntity>(_previousPageToken);

            _previousPageToken = result.PageToken;
            return result.Entries;
        }

        public ValueTask<IChangeTrackingEntry<TEntity>[]> T2(DbContext dbContext, string syncContext) => dbContext.NextHelper<TEntity>(syncContext);
    }

    public interface IChangeSetProcessor<TContext> where TContext : DbContext
    {
        Task ProcessChanges(IEntityType entityType, string syncContext, CancellationToken cancellationToken);
        Task ProcessEntireDataSet(IEntityType entityType, string syncContext, CancellationToken cancellationToken);
    }

    public class ChangeSetProcessor<TContext> : IChangeSetProcessor<TContext> where TContext : DbContext
    {
        IServiceScopeFactory _serviceScopeFactory;
        ILogger<ChangeSetProcessor<TContext>> _logger;

        public ChangeSetProcessor(IServiceScopeFactory serviceScopeFactory, ILogger<ChangeSetProcessor<TContext>>? logger = null)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger ?? NullLogger<ChangeSetProcessor<TContext>>.Instance;
        }

        public Task ProcessChanges(IEntityType entityType, string syncContext, CancellationToken cancellationToken) => ProcessInternal(entityType, syncContext, getNextChangeSetFunc(entityType), ChangeBatchType.Changes, cancellationToken);

        public Task ProcessEntireDataSet(IEntityType entityType, string syncContext, CancellationToken cancellationToken) => ProcessInternal(entityType, syncContext, getEntireDataSetFunc(entityType), ChangeBatchType.DataSet, cancellationToken);

        async Task ProcessInternal(IEntityType entityType, string syncContext, Delegate getNextBatchDelegate, ChangeBatchType changeBatchType, CancellationToken cancellationToken)
        {
            var processBatchMethod = GetType().GetMethod(nameof(processBatch), BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(entityType.ClrType);

            IChangeTrackingEntry[] currentBatch;

            do
            {
                using var scope = _serviceScopeFactory.CreateScope();

                var dbContext = scope.ServiceProvider.GetService<TContext>();
                var changeSetBatchProcessorFactory = scope.ServiceProvider.GetRequiredService<IChangeSetBatchProcessorFactory<TContext>>();

                var logContext = dbContext.GetLogContext();

                logContext.Add(new KeyValuePair<string, object>("SyncContext", syncContext));

                using var logScope = _logger.BeginScope(logContext);

                using var processorContext = new ChangeSetProcessorContext<TContext>(dbContext, syncContext);

                //IDbContextTransaction transaction;

                //if (dbContext.Model.IsSnapshotIsolationEnabled())
                //    transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Snapshot);
                //else
                //    transaction = await dbContext.Database.BeginTransactionAsync();

                //await using var t = transaction;

                currentBatch = await (ValueTask<IChangeTrackingEntry[]>) processBatchMethod.Invoke(this, new[] {syncContext as object, getNextBatchDelegate, dbContext, changeSetBatchProcessorFactory, processorContext, changeBatchType, cancellationToken});

                if (processorContext.RecordCurrentVersion && currentBatch.Any())
                {
                    var maxChangeVersion = currentBatch.Max(e => e.ChangeVersion ?? 0);

                    if (maxChangeVersion > 0)
                        await dbContext.SetLastChangedVersionAsync(entityType, syncContext, maxChangeVersion);
                }

                //await t?.CommitAsync();

            } while (!cancellationToken.IsCancellationRequested && (currentBatch?.Any() ?? false));
        }

        async ValueTask<IChangeTrackingEntry[]> processBatch<TEntity>(
            string syncContext,
            GetNextBatchDelegate<TEntity> getBatchFunc,
            TContext dbContext, 
            IChangeSetBatchProcessorFactory<TContext> changeSetBatchProcessorFactory, 
            ChangeSetProcessorContext<TContext> processorContext,
            ChangeBatchType changeBatchType,
            CancellationToken cancellationToken
            )
        {
            var processors = changeSetBatchProcessorFactory.GetBatchProcessors<TEntity>(syncContext).ToArray();

            var entityType = dbContext.Model.FindEntityType(typeof(TEntity));

            if (!processors.Any())
            {
                _logger.LogDebug("No batch processors found for Entity: {EntityName} in Table: {TableName} for SyncContext: {SyncContext}", entityType.ClrType.Name, entityType.GetFullTableName(), syncContext);
                return new IChangeTrackingEntry[0];
            }

            IChangeTrackingEntry<TEntity>[] batch;

            try
            {
                batch = await getBatchFunc(dbContext, syncContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching batch for Entity: {EntityName} in Table: {TableName}", entityType.ClrType.Name, entityType.GetFullTableName());
                throw;
            }
            
            if (!batch.Any())
            {
                _logger.LogDebug("No items found in batch for Entity: {EntityName} in Table: {TableName}", entityType.ClrType.Name, entityType.GetFullTableName());
                return new IChangeTrackingEntry[0];
            }

            var currentChangeVersion = batch.Max(e => e.ChangeVersion ?? 0);

            var logContext = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("CurrentChangeVersion", currentChangeVersion)
            };

            using var logScope = _logger.BeginScope(logContext);

            _logger.LogDebug("Found {ChangeEntryCount} change(s) in current batch for Entity: {EntityName} in Table: {TableName} at Change Version: {CurrentChangeVersion}", batch.Length, entityType.ClrType.Name, entityType.GetFullTableName(), currentChangeVersion);

            var stopwatch = Stopwatch.StartNew();

            foreach (var changeSetProcessor in processors)
            {
                try
                {
                    _logger.LogDebug("Processing {ChangeEntryCount} change(s) with Change Set Processor: {ChangeSetProcessorName}", batch.Length, changeSetProcessor.GetType().PrettyName());

                    await changeSetProcessor.ProcessBatch(new ChangeBatch<TEntity>(batch, changeBatchType), processorContext, cancellationToken);

                    _logger.LogDebug("{ChangeEntryCount} change(s) successfully processed with Change Set Processor: {ChangeSetProcessorName}", batch.Length, changeSetProcessor.GetType().PrettyName());
                }
                catch (Exception ex) when(_logger.LogErrorWithContext(ex, "Change Set Processor: {ChangeSetProcessorName} failed at Change Version: {CurrentChangeVersion}", changeSetProcessor.GetType().PrettyName(), currentChangeVersion))
                {
                    throw;
                }
            }

            stopwatch.Stop();

            var actualProcessed = batch.Length - processorContext.Skipped;

            _logger.LogInformation("{ChangeEntryCount} change(s) successfully processed for Entity: {EntityName} in Table: {TableName} at Change Version: {CurrentChangeVersion} in: {Elapsed}", actualProcessed, entityType.ClrType.Name, entityType.GetFullTableName(), currentChangeVersion, stopwatch.Elapsed);

            if (processorContext.Skipped > 0)
                _logger.LogInformation("{SkippedChangeEntryCount} change(s) skipped processing for Entity: {EntityName} in Table: {TableName}", processorContext.Skipped, entityType.ClrType.Name, entityType.GetFullTableName());

            return batch;
        }

        Delegate getEntireDataSetFunc(IEntityType entityType)
        {
            var stateType = typeof(DataSetState<>).MakeGenericType(entityType.ClrType);

            var state = Activator.CreateInstance(stateType);

            var dtc = typeof(GetNextBatchDelegate<>).MakeGenericType(entityType.ClrType);

            return Delegate.CreateDelegate(dtc, state, stateType.GetMethod(nameof(DataSetState<object>.T1)));
        }

        Delegate getNextChangeSetFunc(IEntityType entityType)
        {
            var stateType = typeof(DataSetState<>).MakeGenericType(entityType.ClrType);

            var state = Activator.CreateInstance(stateType);

            var dtc = typeof(GetNextBatchDelegate<>).MakeGenericType(entityType.ClrType);

            return Delegate.CreateDelegate(dtc, state, stateType.GetMethod(nameof(DataSetState<object>.T2)));
        }
    }
}
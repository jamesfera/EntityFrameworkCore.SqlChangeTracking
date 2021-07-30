using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkCore.SqlChangeTracking.Extensions;
using EntityFrameworkCore.SqlChangeTracking.SyncEngine.Extensions;
using EntityFrameworkCore.SqlChangeTracking.SyncEngine.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine
{
    public class SyncEngine<TContext> : ISyncEngine where TContext : DbContext
    {
        ILogger<SyncEngine<TContext>> _logger;
        IServiceScopeFactory _serviceScopeFactory;
        IChangeSetProcessor<TContext> _changeSetProcessor;
        IDatabaseChangeMonitor _databaseChangeMonitor;

        List<IDisposable> _changeRegistrations = new List<IDisposable>();

        List<IEntityType> _syncEngineEntityTypes = new List<IEntityType>();

        public string SyncContext { get; }
        public Type DbContextType => typeof(TContext);

        bool _started = false;

        public SyncEngine(
            string syncContext,
            IServiceScopeFactory serviceScopeFactory,
            IDatabaseChangeMonitor databaseChangeMonitor,
            IChangeSetProcessor<TContext> changeSetProcessor,
            ILogger<SyncEngine<TContext>> logger = null)
        {
            SyncContext = syncContext;
            _serviceScopeFactory = serviceScopeFactory;
            _databaseChangeMonitor = databaseChangeMonitor;
            _changeSetProcessor = changeSetProcessor;
            _logger = logger ?? NullLogger<SyncEngine<TContext>>.Instance;
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            //if (string.IsNullOrEmpty(options.SyncContext))
            //    throw new Exception("");

            try
            {
                using var serviceScope = _serviceScopeFactory.CreateScope();

                var dbContext = serviceScope.ServiceProvider.GetRequiredService<TContext>();

                if (!dbContext.Database.IsSqlServer())
                {
                    var ex = new InvalidOperationException("Sync Engine is only compatible with Sql Server.  Configure the DbContext with .UseSqlServer().");

                    _logger.LogCritical(ex, "Error during Sync Engine Initialization");

                    //if (options.ThrowOnStartupException)
                    //    throw ex;

                    return;
                }

                _logger.LogInformation("Initializing Sync Engine with SyncContext: {SyncContext}", SyncContext);

                _syncEngineEntityTypes = dbContext.Model.GetEntityTypes().Where(e => e.IsSyncEngineEnabled() && !e.IsAbstract()).ToList();

                var abstractSyncTypes = dbContext.Model.GetEntityTypes().Where(e => e.IsSyncEngineEnabled() && e.IsAbstract()).ToList();

                foreach (var entityType in abstractSyncTypes)
                {
                    foreach (var type in entityType.GetConcreteDerivedTypesInclusive())
                    {
                        _syncEngineEntityTypes.Add(type);
                    }
                }

                _started = true;

                var databaseName = dbContext.Database.GetDbConnection().Database;

                var connectionString = dbContext.Database.GetDbConnection().ConnectionString;

                foreach (var syncEngineEntityType in _syncEngineEntityTypes)
                {
                    await dbContext.InitializeSyncEngine(syncEngineEntityType, SyncContext).ConfigureAwait(false);
                }

                serviceScope.Dispose();

                _logger.LogInformation("Found {EntityTrackingCount} Entities with Sync Engine enabled for SyncContext: {SyncContext}", _syncEngineEntityTypes.Count, SyncContext);

                foreach (var entityType in _syncEngineEntityTypes)
                {
                    var changeRegistration = _databaseChangeMonitor.RegisterForChanges(o =>
                        {
                            o.TableName = entityType.GetTableName();
                            o.SchemaName = entityType.GetActualSchema();
                            o.DatabaseName = databaseName;
                            o.ConnectionString = connectionString;
                        },
                        t =>
                        {
                            _logger.LogDebug("Received Change notification for Table: {TableName}", entityType.GetFullTableName());

                            return ProcessChanges(entityType);
                        });

                    _changeRegistrations.Add(changeRegistration);

                    _logger.LogInformation("Sync Engine configured for Entity: {EntityTypeName} on Table: {TableName}", entityType.Name, entityType.GetFullTableName());
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error attempting to start Sync Engine for DbContext: {DbContext}", typeof(TContext));

                //if (options.ThrowOnStartupException)
                    throw;
            }
        }

        public async Task ProcessAllChanges()
        {
            var processChangesTasks = _syncEngineEntityTypes.Select(ProcessChanges).ToArray();

            await Task.WhenAll(processChangesTasks).ConfigureAwait(false);
        }

        public async Task ProcessChanges(IEntityType entityType)
        {
            if (!_started)
                throw new InvalidOperationException("Sync Engine has not started.");

            try
            {
                _logger.LogDebug("Processing changes for Entity: {EntityType}", entityType.ClrType);
                await _changeSetProcessor.ProcessChanges(entityType, SyncContext).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Changes for Table: {TableName} for SyncContext: {SyncContext}", entityType.GetFullTableName(), SyncContext);
                throw;
            }
        }

        public Task ProcessChanges<TEntity>()
        {
            return ProcessChanges(typeof(TEntity));
        }

        public Task ProcessChanges(Type clrEntityType)
        {
            var entityType = _syncEngineEntityTypes.FirstOrDefault(e => e.ClrType == clrEntityType);

            if(entityType == null)
                throw new InvalidOperationException($"No Entity Type found for ClrType: {clrEntityType.PrettyName()}");

            return ProcessChanges(entityType);
        }

        public async Task MarkAllEntitiesAsSynced()
        {
            using var serviceScope = _serviceScopeFactory.CreateScope();

            await using var dbContext = serviceScope.ServiceProvider.GetRequiredService<TContext>();

            var currentVersion = await dbContext.GetCurrentChangeTrackingVersion();

            if (!currentVersion.HasValue)
            {
                _logger.LogWarning("Change Tracking is not enabled for this database");
                return;
            }

            foreach (var syncEngineEntityType in _syncEngineEntityTypes)
            {
                await dbContext.SetLastChangedVersionAsync(syncEngineEntityType, SyncContext, currentVersion.Value);
            }
        }

        public async Task MarkEntityAsSynced(IEntityType entityType)
        {
            using var serviceScope = _serviceScopeFactory.CreateScope();

            await using var dbContext = serviceScope.ServiceProvider.GetRequiredService<TContext>();

            var currentVersion = await dbContext.GetCurrentChangeTrackingVersion();

            if (!currentVersion.HasValue)
            {
                _logger.LogWarning("Change Tracking is not enabled for this database");
                return;
            }

            if (!_syncEngineEntityTypes.Contains(entityType))
                throw new InvalidOperationException($"Entity Type: {entityType} does not have sync engine enabled.");

            await dbContext.SetLastChangedVersionAsync(entityType, SyncContext, currentVersion.Value);
        }

        public async Task ResetAllSyncVersions()
        {
            using var serviceScope = _serviceScopeFactory.CreateScope();

            await using var dbContext = serviceScope.ServiceProvider.GetRequiredService<TContext>();

            foreach (var syncEngineEntityType in _syncEngineEntityTypes)
            {
                await dbContext.SetLastChangedVersionAsync(syncEngineEntityType, SyncContext, 0);
            }
        }

        public async Task ResetSyncVersionForEntity(IEntityType entityType)
        {
            using var serviceScope = _serviceScopeFactory.CreateScope();

            await using var dbContext = serviceScope.ServiceProvider.GetRequiredService<TContext>();

            if (!_syncEngineEntityTypes.Contains(entityType))
                throw new InvalidOperationException($"Entity Type: {entityType} does not have sync engine enabled.");

            await dbContext.SetLastChangedVersionAsync(entityType, SyncContext, 0);
        }

        public Task Stop(CancellationToken cancellationToken)
        {
            _changeRegistrations.ForEach(r => r.Dispose());

            _logger.LogInformation("Shutting down Sync Engine.");

            _started = false;

            return Task.CompletedTask;
        }
    }
}
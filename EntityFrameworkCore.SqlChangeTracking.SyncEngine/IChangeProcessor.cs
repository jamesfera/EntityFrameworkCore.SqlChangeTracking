﻿using EntityFrameworkCore.SqlChangeTracking.SyncEngine.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;


namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine
{
    public interface IChangeSetProcessorRegistration
    {
        (string syncContext, Type processorType, Func<Type, bool> entityTypeFilter) Registration { get; }
    }

    public class ChangeSetProcessorRegistration : IChangeSetProcessorRegistration
    {
        public ChangeSetProcessorRegistration((string syncContext, Type processorType, Func<Type, bool> entityTypeFilter) registration)
        {
            Registration = registration;
        }

        public (string syncContext, Type processorType, Func<Type, bool> entityTypeFilter) Registration { get; }
    }

    public interface IProcessorTypeRegistry<TContext> where TContext : DbContext
    {
        (Type processorType, Func<Type, bool> entityTypeFilter)[] GetProcessorTypesForSyncContext(string syncContext);
        Type[] GetProcessorTypesForEntity(Type entityType, string syncContext);
        bool HasBatchProcessor(Type entityType, string syncContext);
    }

    public class ProcessorTypeRegistry<TContext> : IProcessorTypeRegistry<TContext> where TContext : DbContext
    {
        Dictionary<string, (Type processorType, Func<Type, bool> entityTypeFilter)[]> _registrations;

        public ProcessorTypeRegistry(IEnumerable<IChangeSetProcessorRegistration> registrations)
        {
            _registrations = registrations.Select(r => r.Registration).GroupBy(r => r.syncContext, r => (r.processorType, r.entityTypeFilter)).ToDictionary(g => g.Key, g => g.Distinct().ToArray());
        }

        public Type[] GetProcessorTypesForEntity(Type entityType, string syncContext)
        {
            var entityTypesToMatch = entityType.GetAssignableTypesForEntity();

            var entityProcessorTypes = GetProcessorTypesForSyncContext(syncContext).Where(p =>
            {
                var processorEntityTypes = p.processorType.GetTypesForChangeProcessor<TContext>().Where(t => p.entityTypeFilter(t)).ToArray();
                return entityTypesToMatch.Intersect(processorEntityTypes).Any();
            })
                .Select(p => p.processorType)
                .ToArray();

            return entityProcessorTypes;
        }

        public (Type processorType, Func<Type, bool> entityTypeFilter)[] GetProcessorTypesForSyncContext(string syncContext)
        {
            if (!_registrations.TryGetValue(syncContext, out (Type processorType, Func<Type, bool> entityTypeFilter)[] serviceTypes))
                _registrations.Add(syncContext, serviceTypes = Array.Empty<(Type processorType, Func<Type, bool> entityTypeFilter)>());

            return serviceTypes;
        }

        public bool HasBatchProcessor(Type entityType, string syncContext)
        {
            return GetProcessorTypesForEntity(entityType, syncContext).Any();
        }
    }

    public class ChangeSetBatchProcessorFactory<TContext> : IChangeSetBatchProcessorFactory<TContext> where TContext : DbContext
    {
        IServiceProvider _serviceProvider;
        IProcessorTypeRegistry<TContext> _processorTypeRegistry;

        public ChangeSetBatchProcessorFactory(IServiceProvider serviceProvider, IProcessorTypeRegistry<TContext> processorTypeRegistry)
        {
            _serviceProvider = serviceProvider;
            _processorTypeRegistry = processorTypeRegistry;
        }

        public IEnumerable<IChangeSetBatchProcessor<TEntity, TContext>> GetBatchProcessors<TEntity>(string syncContext)
        {
            var entityProcessorTypes = _processorTypeRegistry.GetProcessorTypesForEntity(typeof(TEntity), syncContext);

            var services = entityProcessorTypes.Select(t => (IChangeSetBatchProcessor<TEntity, TContext>)_serviceProvider.GetService(t)).ToArray();

            return services;
        }
    }

    //public interface IBatchProcessorManager<TContext> where TContext : DbContext
    //{
    //    ValueTask<bool> ProcessBatch<TEntity>(string syncContext, Func<TContext, string, ValueTask<IChangeTrackingEntry<TEntity>[]>> getBatchFunc, Func<IChangeSetProcessorContext<TContext>, IChangeTrackingEntry<TEntity>[], ValueTask>? batchCompleteFunc = null);
    //}

    //public class BatchProcessorManager<TContext> : IBatchProcessorManager<TContext> where TContext : DbContext
    //{
    //    readonly TContext _dbContext;
    //    readonly IChangeSetBatchProcessorFactory<TContext> _changeSetBatchProcessorFactory;
    //    readonly ILogger<BatchProcessorManager<TContext>> _logger;

    //    public BatchProcessorManager(TContext dbContext, IChangeSetBatchProcessorFactory<TContext> changeSetBatchProcessorFactory, ILogger<BatchProcessorManager<TContext>> logger = null)
    //    {
    //        _dbContext = dbContext;
    //        _changeSetBatchProcessorFactory = changeSetBatchProcessorFactory;
    //        _logger = logger ?? NullLogger<BatchProcessorManager<TContext>>.Instance;
    //    }

    //    public async ValueTask<bool> ProcessBatch<TEntity>(string syncContext, Func<TContext, string, ValueTask<IChangeTrackingEntry<TEntity>[]>> getBatchFunc, Func<IChangeSetProcessorContext<TContext>, IChangeTrackingEntry<TEntity>[], ValueTask>? batchCompleteFunc = null)
    //    {
    //        var processors = _changeSetBatchProcessorFactory.GetBatchProcessors<TEntity>(syncContext).ToArray();

    //        if (!processors.Any())
    //        {
    //            _logger.LogWarning("No batch processors found for Entity: {EntityType} for SyncContext: {SyncContext}", typeof(TEntity), syncContext);
    //            return false;
    //        }

    //        var dbContext = _dbContext;

    //        var entityType = dbContext.Model.FindEntityType(typeof(TEntity));
            
    //        var batch = await getBatchFunc(dbContext, syncContext);

    //        if (!batch.Any())
    //        {
    //            _logger.LogDebug("No items found in batch.");
    //            return false;
    //        }

    //        _logger.LogInformation("Found {ChangeEntryCount} change(s) in current batch for Table: {TableName}", batch.Length, entityType.GetFullTableName());

    //        using var processorContext = new ChangeSetProcessorContext<TContext>(dbContext, syncContext);

    //        foreach (var changeSetProcessor in processors)
    //        {
    //            try
    //            {
    //                await changeSetProcessor.ProcessBatch(batch, processorContext);
    //            }
    //            catch (Exception ex)
    //            {
    //                throw;
    //            }
    //        }

    //        if (batchCompleteFunc != null)
    //            await batchCompleteFunc.Invoke(processorContext, batch);

    //        _logger.LogInformation("Successfully processed {ChangeEntryCount} change entries for Table: {TableName}", batch.Length, entityType.GetFullTableName());

    //        return true;
    //    }

    //    //ChangeTrackingEntry convert(Type interfaceType, Type concreteType, object entry)
    //    //{
    //    //    var withTypeMethod = typeof(ChangeTrackingEntry<>).MakeGenericType(concreteType).GetMethod(nameof(ChangeTrackingEntry<object>.WithType));
    //    //    var method = withTypeMethod.MakeGenericMethod(interfaceType);

    //    //    var entityParam = Expression.Parameter(entry.GetType(), "entity");
    //    //    var withTypeCall = Expression.Call(entityParam, method);

    //    //    var lambda = Expression.Lambda(withTypeCall, entityParam).Compile();

    //    //    return lambda.DynamicInvoke(entry) as ChangeTrackingEntry;
    //    //}

    //    //Func<TContext, long, int, IEnumerable<ChangeTrackingEntry>> getChangesFunc(IEntityType entityType)
    //    //{
    //    //    var getChangesMethodInfo = typeof(Extensions.DbSetExtensions).GetMethod(nameof(Extensions.DbSetExtensions
    //    //        .GetChangesSinceVersion), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(entityType.ClrType);

    //    //    var dbSetType = typeof(DbSet<>).MakeGenericType(entityType.ClrType);

    //    //    var dbSetPropertyInfo = typeof(TContext).GetProperties().FirstOrDefault(p => p.PropertyType == dbSetType);

    //    //    if (dbSetPropertyInfo == null)
    //    //        throw new Exception($"No Property of type {dbSetType.PrettyName()} found on {typeof(TContext).Name}");

    //    //    var dbContextParameterExpression = Expression.Parameter(typeof(TContext), "dbContext");
    //    //    var dbSetPropertyExpression = Expression.Property(dbContextParameterExpression, dbSetPropertyInfo);

    //    //    var versionParameter = Expression.Parameter(typeof(long), "version");
    //    //    var maxResultsParameter = Expression.Parameter(typeof(int), "maxResults");
            
    //    //    var getChangesCallExpression = Expression.Call(getChangesMethodInfo, dbSetPropertyExpression, versionParameter, maxResultsParameter);

    //    //    var lambda = Expression.Lambda<Func<TContext, long, int, IEnumerable<ChangeTrackingEntry>>>(getChangesCallExpression, dbContextParameterExpression, versionParameter, maxResultsParameter);

    //    //    return lambda.Compile();
    //    //}
    
    //}
}

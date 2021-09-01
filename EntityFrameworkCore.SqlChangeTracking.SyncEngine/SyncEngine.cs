using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        IDatabaseChangeMonitorManager _databaseChangeMonitorManager;

        List<IAsyncDisposable> _changeRegistrations = new List<IAsyncDisposable>();

        List<IEntityType> _syncEngineEntityTypes = new List<IEntityType>();

        public IReadOnlyList<IEntityType> SyncEntityTypes => _syncEngineEntityTypes;

        public string SyncContext { get; }
        public Type DbContextType => typeof(TContext);

        bool _started = false;

        public SyncEngine(
            string syncContext,
            IServiceScopeFactory serviceScopeFactory,
            IDatabaseChangeMonitorManager databaseChangeMonitorManager,
            IChangeSetProcessor<TContext> changeSetProcessor,
            ILogger<SyncEngine<TContext>> logger = null)
        {
            SyncContext = syncContext;
            _serviceScopeFactory = serviceScopeFactory;
            _databaseChangeMonitorManager = databaseChangeMonitorManager;
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

                var processorRegistry = serviceScope.ServiceProvider.GetRequiredService<IProcessorTypeRegistry<TContext>>();

                _logger.LogInformation("Initializing Sync Engine with SyncContext: {SyncContext}", SyncContext);

                _syncEngineEntityTypes = dbContext.Model.GetEntityTypes().Where(e => e.IsSyncEngineEnabled() && !e.IsAbstract() && processorRegistry.HasBatchProcessor(e.ClrType, SyncContext)).ToList();

                var abstractSyncTypes = dbContext.Model.GetEntityTypes().Where(e => e.IsSyncEngineEnabled() && e.IsAbstract()).ToList();

                foreach (var entityType in abstractSyncTypes)
                {
                    foreach (var type in entityType.GetConcreteDerivedTypesInclusive().Where(e => processorRegistry.HasBatchProcessor(e.ClrType, SyncContext)))
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

                var databaseChangeMonitor = _databaseChangeMonitorManager.GetChangeMonitor(databaseName, createIfNotExist: true);

                var assemblyName = Assembly.GetEntryAssembly().GetName().Name.Split(".");

                var applicationName = assemblyName.Skip(assemblyName.Length - 1).FirstOrDefault();
                
                foreach (var entityType in _syncEngineEntityTypes)
                {
                    var changeRegistration = databaseChangeMonitor.RegisterForChanges(o =>
                        {
                            o.ApplicationName = applicationName;
                            o.TableName = entityType.GetTableName();
                            o.SchemaName = entityType.GetActualSchema();
                            o.ConnectionString = connectionString;

                            o.OnTableChanged = n =>
                            {
                                _logger.LogDebug("Received Change notification for Table: {TableName} in Database: {DatabaseName}", n.Table, n.Database);

                                return ProcessChanges(entityType);
                            };

                            o.OnChangeMonitorTerminated = (monitor, table, application, ex) =>
                            {
                                if (ex == null)
                                    _logger.LogInformation("Table change listener terminated for table: {TableName} database: {DatabaseName}", table, monitor.DatabaseName);
                                else
                                    _logger.LogCritical(ex, "Table change listener terminated for table: {TableName} database: {DatabaseName}", table, monitor.DatabaseName);

                                return Task.CompletedTask;
                            };
                        });

                    _changeRegistrations.Add(changeRegistration);

                    _logger.LogInformation("Sync Engine configured for Entity: {EntityTypeName} on Table: {TableName}", entityType.Name, entityType.GetFullTableName());
                }

                await databaseChangeMonitor.Enable();
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

            validateEntityType(entityType);

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

        public Task ProcessChanges(string entityTypeName)
        {
            var entityType = validateEntityName(entityTypeName);

            return ProcessChanges(entityType);
        }

        public async Task ProcessDataSet(IEntityType entityType)
        {
            validateEntityType(entityType);

            try
            {
                _logger.LogDebug("Processing entire data set for Entity: {EntityType}", entityType.ClrType);
               
                await _changeSetProcessor.ProcessEntireDataSet(entityType, SyncContext).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing data set for Table: {TableName} for SyncContext: {SyncContext}", entityType.GetFullTableName(), SyncContext);
                throw;
            }
        }

        public Task ProcessDataSet(string entityTypeName)
        {
            var entityType = validateEntityName(entityTypeName);

            return ProcessDataSet(entityType);
        }

        public async Task MarkEntityAsSynced(IEntityType entityType)
        {
            validateEntityType(entityType);

            using var serviceScope = _serviceScopeFactory.CreateScope();

            await using var dbContext = serviceScope.ServiceProvider.GetRequiredService<TContext>();

            var currentVersion = await dbContext.GetCurrentChangeTrackingVersion();

            if (!currentVersion.HasValue)
            {
                _logger.LogWarning("Change Tracking is not enabled for this database");
                return;
            }

            await dbContext.SetLastChangedVersionAsync(entityType, SyncContext, currentVersion.Value);
        }

        public Task MarkEntityAsSynced(string entityName) => MarkEntityAsSynced(validateEntityName(entityName));

        public async Task SetChangeVersion(IEntityType entityType, long changeVersion)
        {
            validateEntityType(entityType);

            using var serviceScope = _serviceScopeFactory.CreateScope();

            await using var dbContext = serviceScope.ServiceProvider.GetRequiredService<TContext>();

            await dbContext.SetLastChangedVersionAsync(entityType, SyncContext, changeVersion);
        }

        public Task SetChangeVersion(string entityName, long changeVersion) => SetChangeVersion(validateEntityName(entityName), changeVersion);

        public async Task Stop(CancellationToken cancellationToken)
        {
            foreach (var changeRegistration in _changeRegistrations)
            {
                await changeRegistration.DisposeAsync();
            }

            _logger.LogInformation("Shutting down Sync Engine.");

            _started = false;
        }

        IEntityType validateEntityName(string entityName)
        {
            var entityType = _syncEngineEntityTypes.FirstOrDefault(e => e.ClrType.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));

            if (entityType == null)
                throw new InvalidOperationException($"Entity Type: '{entityName}' does not have sync engine enabled.");

            validateEntityType(entityType);

            return entityType;
        }

        void validateEntityType(IEntityType entityType)
        {
            if (!_syncEngineEntityTypes.Any(e => e == entityType))
                throw new InvalidOperationException($"Entity Type: {entityType} does not have sync engine enabled.");
        }
    }
}
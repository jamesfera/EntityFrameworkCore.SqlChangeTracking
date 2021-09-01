﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkCore.SqlChangeTracking.SyncEngine.Extensions;
using EntityFrameworkCore.SqlChangeTracking.SyncEngine.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine.Monitoring
{
    public interface IDatabaseChangeMonitorManager
    {
        IDatabaseChangeMonitor GetChangeMonitor(string databaseName, bool createIfNotExist = false);

        IEnumerable<IDatabaseChangeMonitor> GetAllChangeMonitors();

        void PauseAll();
        void ResumeAll();

        Task EnableAll();
        Task DisableAll();
    }

    class DatabaseChangeMonitorManager : IDatabaseChangeMonitorManager
    {
        ConcurrentDictionary<string, IDatabaseChangeMonitor> _changeMonitorsDictionary = new ConcurrentDictionary<string, IDatabaseChangeMonitor>();

        ILoggerFactory _loggerFactory;

        public DatabaseChangeMonitorManager(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public IDatabaseChangeMonitor GetChangeMonitor(string databaseName, bool createIfNotExist)
        {
            return _changeMonitorsDictionary.GetOrAdd(databaseName, d =>
            {
                if (createIfNotExist)
                    return new DatabaseChangeMonitor(databaseName, _loggerFactory);

                throw new Exception($"Change Monitor for Database {databaseName} not found");
            });
        }

        public IEnumerable<IDatabaseChangeMonitor> GetAllChangeMonitors()
        {
            return _changeMonitorsDictionary.Values;
        }

        public void PauseAll()
        {
            foreach (var databaseChangeMonitor in _changeMonitorsDictionary.Values)
            {
                databaseChangeMonitor.Pause();
            }
        }

        public void ResumeAll()
        {
            foreach (var databaseChangeMonitor in _changeMonitorsDictionary.Values)
            {
                databaseChangeMonitor.Resume();
            }
        }

        public async Task EnableAll()
        {
            foreach (var databaseChangeMonitor in _changeMonitorsDictionary.Values)
            {
                await databaseChangeMonitor.Enable();
            }
        }

        public async Task DisableAll()
        {
            foreach (var databaseChangeMonitor in _changeMonitorsDictionary.Values)
            {
                await databaseChangeMonitor.Disable();
            }
        }
    }

    public interface IDatabaseChangeMonitor
    {
        IAsyncDisposable RegisterForChanges(Action<DatabaseChangeMonitorRegistrationOptions> optionsBuilder);

        string DatabaseName { get; }

        void Pause();
        void Resume();

        Task Enable();
        Task Disable();
    }

    public class DatabaseChangeMonitor : IDatabaseChangeMonitor, IAsyncDisposable
    {
        ILogger<DatabaseChangeMonitor> _logger;
        ILoggerFactory _loggerFactory;

        public string DatabaseName { get; }

        ConcurrentDictionary<string, Task<SqlDependencyEx>> _sqlDependencies = new ConcurrentDictionary<string, Task<SqlDependencyEx>>();
        ConcurrentDictionary<string, ImmutableList<ChangeRegistration>> _registeredChangeActions = new ConcurrentDictionary<string, ImmutableList<ChangeRegistration>>();

        bool _paused = false;

        protected internal DatabaseChangeMonitor(string databaseName, ILoggerFactory loggerFactory = null)
        {
            _logger = loggerFactory?.CreateLogger<DatabaseChangeMonitor>() ?? NullLogger<DatabaseChangeMonitor>.Instance;
            _loggerFactory = loggerFactory;

            DatabaseName = databaseName;
        }

        public IAsyncDisposable RegisterForChanges(Action<DatabaseChangeMonitorRegistrationOptions> optionsBuilder)
        {
            var options = new DatabaseChangeMonitorRegistrationOptions();

            optionsBuilder.Invoke(options);

            if (string.IsNullOrWhiteSpace(options.ApplicationName))
                throw new ArgumentNullException(nameof(DatabaseChangeMonitorRegistrationOptions.ApplicationName));

            if (string.IsNullOrWhiteSpace(options.TableName))
                throw new ArgumentNullException(nameof(DatabaseChangeMonitorRegistrationOptions.TableName));

            if (string.IsNullOrWhiteSpace(options.ConnectionString))
                throw new ArgumentNullException(nameof(DatabaseChangeMonitorRegistrationOptions.ConnectionString));

            if (string.IsNullOrWhiteSpace(options.SchemaName))
                throw new ArgumentNullException(nameof(DatabaseChangeMonitorRegistrationOptions.SchemaName));

            if (options.OnTableChanged == null)
                throw new ArgumentNullException(nameof(DatabaseChangeMonitorRegistrationOptions.OnTableChanged));

            var changeRegistration = new ChangeRegistration(options, async registration =>
            {
                var registrations = _registeredChangeActions.AddOrUpdate(registration.RegistrationKey, new List<ChangeRegistration>().ToImmutableList(), (k, l) => l.Remove(registration));

                if (!registrations.Any())
                    await removeTableChangeListener(registration.RegistrationKey);
            });

            _registeredChangeActions.AddOrUpdate(changeRegistration.RegistrationKey, new List<ChangeRegistration>(new[] { changeRegistration }).ToImmutableList(), (k, r) => r.Add(changeRegistration));

            return changeRegistration;
        }

        public void Pause()
        {
            _logger.LogInformation("Change Notifications Paused for Database: {DatabaseName}", DatabaseName);
            _paused = true;
        }

        public void Resume()
        {
            _logger.LogInformation("Change Notifications Resumed for Database: {DatabaseName}", DatabaseName);
            _paused = false;
        }

        public async Task Enable()
        {
            foreach (var changeRegistration in _registeredChangeActions.Values.SelectMany(v => v))
            {
                var registrationKey = changeRegistration.RegistrationKey;

                var options = changeRegistration.Options;

                await _sqlDependencies.GetOrAdd(registrationKey, async k =>
                {
                    var fullTableName = $"{options.SchemaName}.{options.TableName}";

                    var sqlTableDependency = new SqlDependencyEx(registrationKey, _loggerFactory?.CreateLogger<SqlDependencyEx>() ?? NullLogger<SqlDependencyEx>.Instance, options.ConnectionString, DatabaseName, options.TableName, options.SchemaName, receiveDetails: true);

                    await sqlTableDependency.Start(TableChangedEventHandler, async (sqlEx, ex) =>
                    {
                        if (changeRegistration.Options.OnChangeMonitorTerminated != null)
                            await changeRegistration.Options.OnChangeMonitorTerminated.Invoke(this, fullTableName, options.ApplicationName, ex);
                    });

                    _logger.LogInformation("Created Change Event Listener in Database: {DatabaseName} for table {TableName} with identity: {SqlDependencyId}", DatabaseName, fullTableName, registrationKey);

                    return sqlTableDependency;
                });
            }
        }

        public async Task Disable()
        {
            foreach (var registrationKey in _sqlDependencies.Keys)
            {
                await removeTableChangeListener(registrationKey);
            }
        }

        async Task removeTableChangeListener(string registrationKey)
        {
            if (!_sqlDependencies.TryRemove(registrationKey, out Task<SqlDependencyEx> sqlExTask))
                return;

            var sqlEx = sqlExTask.Result;

            await sqlEx.Stop();
        }

        async Task TableChangedEventHandler(SqlDependencyEx sqlEx, SqlDependencyEx.TableChangedEventArgs e)
        {
            string? tableName = null;

            try
            {
                tableName = $"{sqlEx.SchemaName}.{sqlEx.TableName}";

                if (_paused)
                {
                    _logger.LogDebug("Database Change Monitor paused.  Skipping change notification for table: {TableName}", tableName);
                    return;
                }

                _logger.LogDebug("Change detected in table: {TableName} ", tableName);

                var registrationKey = sqlEx.Identity;

                if (_registeredChangeActions.TryGetValue(registrationKey, out ImmutableList<ChangeRegistration> actions))
                {
                    //await _semaphore.WaitAsync();
                    
                    var tasks = actions.Select(async a =>
                    {
                        var notification = new TableChangedNotification(sqlEx.DatabaseName, sqlEx.TableName, sqlEx.SchemaName, e.NotificationType.ToChangeOperation());

                        try
                        {
                            await a.ChangeFunc(notification);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error handling notification: {TableChangedNotification} Handler: {NotificationHandler}", notification, a.GetType().PrettyName());
                        }
                    });

                    try
                    {
                        await Task.WhenAll(tasks);
                    }
                    finally
                    {
                        //_semaphore.Release();
                    }
                }
                else //this should never happen
                {
                    _logger.LogWarning("Unable to process Table Changed Event. No Change Registration found for Key: {RegistrationKey}", registrationKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Change Event for Table: {TableName}", tableName);
            }
        }

        bool _disposed;
        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                await Disable();

                _registeredChangeActions = null;

                _disposed = true;
            }
        }

        class ChangeRegistration : IAsyncDisposable
        {
            public string RegistrationKey { get; }

            public DatabaseChangeMonitorRegistrationOptions Options { get; }

            public Func<ITableChangedNotification, Task> ChangeFunc => Options.OnTableChanged;

            Func<ChangeRegistration, Task> _registrationRemovedAction;

            public ChangeRegistration(DatabaseChangeMonitorRegistrationOptions options, Func<ChangeRegistration, Task> registrationRemovedAction)
            {
                Options = options;

                RegistrationKey = $"{options.ApplicationName}_{options.SchemaName}_{options.TableName}".ToLowerInvariant();

                _registrationRemovedAction = registrationRemovedAction;
            }

            public async ValueTask DisposeAsync()
            {
                await _registrationRemovedAction(this);
            }
        }
    }

    public class DatabaseChangeMonitorRegistrationOptions
    {
        public string ApplicationName { get; set; }

        public string TableName { get; set; }
        public string SchemaName { get; set; } = "dbo";
        public string ConnectionString { get; set; }

        public Func<ITableChangedNotification, Task> OnTableChanged { get; set; }

        public Func<IDatabaseChangeMonitor, string, string, Exception?, Task>? OnChangeMonitorTerminated { get; set; }
    }
}

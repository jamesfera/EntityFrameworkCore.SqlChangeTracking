﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityFrameworkCore.SqlChangeTracking.SyncEngine.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine
{
    public interface ISyncManager
    {
        ISyncEngine[] GetInstances();
    }

    public interface ISyncEngineManager: ISyncManager
    {
        ISyncEngine CreateSyncEngine<TContext>(string syncContext) where TContext : DbContext;
        //void AddSyncEngine(ISyncEngine syncEngine, string syncContext);
    }

    public class SyncEngineManager : ISyncEngineManager
    {
        IServiceProvider _serviceProvider;
        ILogger<SyncEngineManager> _logger;
        ConcurrentDictionary<string, ISyncEngine> _syncEngineInstances = new ConcurrentDictionary<string, ISyncEngine>();

        public SyncEngineManager(IServiceProvider serviceProvider, ILogger<SyncEngineManager>? logger = null)
        {
            _serviceProvider = serviceProvider;
            _logger = logger ?? NullLogger<SyncEngineManager>.Instance;
        }

        public ISyncEngine CreateSyncEngine<TContext>(string syncContext) where TContext : DbContext
        {
            var key = $"{typeof(TContext)}:{syncContext}";

            var syncEngine = _syncEngineInstances.GetOrAdd(key, k => new SyncEngine<TContext>(
                syncContext,
                _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                _serviceProvider.GetRequiredService<IDatabaseChangeMonitor>(),
                _serviceProvider.GetRequiredService<IChangeSetProcessor<TContext>>(),
                _serviceProvider.GetService<ILogger<SyncEngine<TContext>>>()));

            return syncEngine;
        }

        public void AddSyncEngine(ISyncEngine syncEngine, string syncContext)
        {
            var key = $"{syncEngine.DbContextType}{syncContext}";

            _syncEngineInstances.TryAdd(key, syncEngine);
        }

        public ISyncEngine[] GetInstances() => _syncEngineInstances.Values.ToArray();



    }
}

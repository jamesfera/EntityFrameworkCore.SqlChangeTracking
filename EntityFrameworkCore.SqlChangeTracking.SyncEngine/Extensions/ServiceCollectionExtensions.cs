using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EntityFrameworkCore.SqlChangeTracking.SyncEngine.Extensions;
using EntityFrameworkCore.SqlChangeTracking.SyncEngine.Monitoring;
using EntityFrameworkCore.SqlChangeTracking.SyncEngine.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSyncEngine<TContext>(this IServiceCollection services, string syncContext, Func<Type, bool> processorTypePredicateFunc, params Assembly[] assembliesToScan) where TContext : DbContext
        {
            services.TryAddScoped<IChangeSetBatchProcessorFactory<TContext>, ChangeSetBatchProcessorFactory<TContext>>(); 
            //services.TryAddScoped<IBatchProcessorManager<TContext>, BatchProcessorManager<TContext>>();

            services.TryAddSingleton<IDatabaseChangeMonitor, DatabaseChangeMonitor>();
            services.TryAddSingleton<IChangeSetProcessor<TContext>, ChangeSetProcessor<TContext>>();
            services.TryAddSingleton<IProcessorTypeRegistry<TContext>, ProcessorTypeRegistry<TContext>>();

            services.TryAddSingleton<ISyncEngineManager, SyncEngineManager>();
            services.TryAddSingleton<ISyncManager>(s => s.GetRequiredService<ISyncEngineManager>());

            foreach (var assembly in assembliesToScan)
            {
                var typesToScan = assembly.GetTypes().Where(processorTypePredicateFunc);

                var processors = typesToScan.Where(t => t.IsChangeProcessor<TContext>()).ToArray();

                foreach (var processorType in processors)
                {
                    var serviceTypes = processorType.GetChangeProcessorInterfaces<TContext>();

                    foreach (var serviceType in serviceTypes)
                    {
                        services.TryAddScoped(serviceType, processorType);
                        services.AddSingleton<IChangeSetProcessorRegistration>(new ChangeSetProcessorRegistration(new KeyValuePair<string, Type>(syncContext, serviceType)));
                    }
                }
            }

            return services;
        }
    }
}

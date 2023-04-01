using System.Reflection;
using EntityFrameworkCore.SqlChangeTracking.SyncEngine.Extensions;
using EntityFrameworkCore.SqlChangeTracking.SyncEngine.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSyncEngine<TContext>(this IServiceCollection services, string syncContext, Func<Type, bool> entityTypeFilter, params Assembly[] assembliesToScan) where TContext : DbContext
        {
            services.TryAddScoped<IChangeSetBatchProcessorFactory<TContext>, ChangeSetBatchProcessorFactory<TContext>>();

            services.TryAddSingleton<IDatabaseChangeMonitorManager, DatabaseChangeMonitorManager>();

            services.TryAddSingleton<IChangeSetProcessor<TContext>, ChangeSetProcessor<TContext>>();
            services.TryAddSingleton<IProcessorTypeRegistry<TContext>, ProcessorTypeRegistry<TContext>>();

            services.TryAddSingleton<ISyncEngineManager, SyncEngineManager>();
            services.TryAddSingleton<ISyncManager>(s => s.GetRequiredService<ISyncEngineManager>());

            foreach (var assembly in assembliesToScan)
            {
                var processors = assembly.GetTypes().Where(t => t.IsChangeProcessor<TContext>() && t.GetChangeProcessorInterfaces<TContext>().Any(i => entityTypeFilter(i.GetGenericArguments()[0]))).ToArray();

                foreach (var processorType in processors)
                {
                    services.AddChangeSetProcessor(syncContext, processorType, entityTypeFilter);
                }
            }

            return services;
        }

        public static IServiceCollection AddChangeSetProcessor(this IServiceCollection services, string syncContext, Type processorType, Func<Type, bool>? entityTypeFilter = null)
        {
            entityTypeFilter ??= type => true;

            services.TryAddScoped(processorType);
            services.AddSingleton<IChangeSetProcessorRegistration>(new ChangeSetProcessorRegistration((syncContext, processorType, entityTypeFilter)));

            return services;
        }
    }
}

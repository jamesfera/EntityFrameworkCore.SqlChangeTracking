﻿using System;
using System.Reflection;
using EntityFrameworkCore.SqlChangeTracking.SyncEngine.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHostedSyncEngineService<TContext>(this IServiceCollection services, Action<SyncEngineOptions>? optionsBuilder, Func<Type, bool>? entityTypeFilter, params Assembly[] assemblies) where TContext : DbContext
        {
            var options = new SyncEngineOptions();

            optionsBuilder?.Invoke(options);

            entityTypeFilter ??= type => true;

            services.AddSingleton<IHostedService>(s => new SyncEngineHostedService(s.GetRequiredService<ISyncEngineManager>().CreateSyncEngine<TContext>(options)));
            services.AddSyncEngine<TContext>(options.SyncContext, entityTypeFilter, assemblies);

            return services;
        }

        public static IServiceCollection AddHostedSyncEngineService<TContext>(this IServiceCollection services, Action<SyncEngineOptions> optionsBuilder, params Assembly[] assemblies) where TContext : DbContext
        {
            return services.AddHostedSyncEngineService<TContext>(optionsBuilder, null, assemblies);
        }

        public static IServiceCollection AddHostedSyncEngineService<TContext>(this IServiceCollection services, params Assembly[] assemblies) where TContext : DbContext
        {
            return services.AddHostedSyncEngineService<TContext>(null, null, assemblies);
        }

        //public static IServiceCollection AddHostedSyncEngineService<TContext>(this IServiceCollection services, Func<Type, bool> processorTypePredicate, params Assembly[] assemblies) where TContext : DbContext
        //{
        //    return services.AddHostedSyncEngineService<TContext>(null, processorTypePredicate, assemblies);
        //}
    }
}

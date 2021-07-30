using System;
using EntityFrameworkCore.SqlChangeTracking.Options;
using EntityFrameworkCore.SqlChangeTracking.SyncEngine.Models;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine
{
    public static class ModelBuilderExtensions
    {
        public static ModelBuilder ConfigureSyncEngine(this ModelBuilder modelBuilder, Action<SqlChangeTrackingModelOptions>? configBuilderAction = null)
        {
            modelBuilder.ConfigureChangeTracking(configBuilderAction);

            modelBuilder.ApplyConfiguration(new LastSyncedChangeVersion());
            
            return modelBuilder;
        }
    }
}

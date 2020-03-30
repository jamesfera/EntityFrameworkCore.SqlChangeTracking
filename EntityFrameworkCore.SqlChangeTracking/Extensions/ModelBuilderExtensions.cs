using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class ModelBuilderExtensions
    {
        public static ModelBuilder ConfigureChangeTracking(this ModelBuilder modelBuilder, Action<ChangeTrackingConfigurationBuilder>? configBuilderAction = null)
        {
            var configBuilder = new ChangeTrackingConfigurationBuilder();

            configBuilderAction?.Invoke(configBuilder);
            
            modelBuilder.Model.EnableChangeTracking();

            if(configBuilder.EnableSnapshotIsolation) 
                modelBuilder.Model.EnableSnapshotIsolation();

            modelBuilder.Model.SetChangeTrackingRetentionDays(configBuilder.RetentionDays);
            modelBuilder.Model.SetChangeTrackingAutoCleanupEnabled(configBuilder.AutoCleanUp);
            
            return modelBuilder;
        }
    }

    public class ChangeTrackingConfigurationBuilder
    {
        public bool EnableSnapshotIsolation { get; set; } = true;
        public int RetentionDays { get; set; } = 2;
        public bool AutoCleanUp { get; set; } = true;
    }
}

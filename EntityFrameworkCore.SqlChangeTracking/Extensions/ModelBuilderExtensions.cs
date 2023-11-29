using EntityFrameworkCore.SqlChangeTracking.Options;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class ModelBuilderExtensions
    {
        public static ModelBuilder ConfigureChangeTracking(this ModelBuilder modelBuilder, Action<SqlChangeTrackingModelOptions>? configBuilderAction = null)
        {
            var configBuilder = new SqlChangeTrackingModelOptions();

            configBuilderAction?.Invoke(configBuilder);
            
            modelBuilder.Model.EnableChangeTracking();

            if(configBuilder.EnableSnapshotIsolation) 
                modelBuilder.Model.EnableSnapshotIsolation();

            modelBuilder.Model.SetChangeTrackingRetentionDays(configBuilder.RetentionDays);
            modelBuilder.Model.SetChangeTrackingAutoCleanupEnabled(configBuilder.AutoCleanUp);
            
            return modelBuilder;
        }
    }
}

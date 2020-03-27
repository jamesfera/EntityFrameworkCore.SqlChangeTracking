using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class ModelBuilderExtensions
    {
        public static ModelBuilder ConfigureChangeTracking(this ModelBuilder modelBuilder, bool enableSnapshotIsolation = true, int retentionDays = 2, bool autoCleanUp = true)
        {
            modelBuilder.Model.SetChangeTrackingEnabled(true);
            modelBuilder.Model.SetSnapshotIsolationEnabled(true);
            modelBuilder.Model.SetChangeTrackingRetentionDays(retentionDays);
            modelBuilder.Model.SetChangeTrackingAutoCleanupEnabled(autoCleanUp);

            return modelBuilder;
        }
    }
}

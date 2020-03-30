using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class ModelExtensions
    {
        public static bool IsChangeTrackingEnabled(this IModel model)
            => model[SqlChangeTrackingAnnotationNames.Enabled] as bool? ?? false;

        public static void EnableChangeTracking(this IMutableModel model)
            => model.SetOrRemoveAnnotation(SqlChangeTrackingAnnotationNames.Enabled, true);

        public static bool IsSnapshotIsolationEnabled(this IModel model)
            => model[SqlChangeTrackingAnnotationNames.SnapshotIsolation] as bool? ?? false;

        public static void EnableSnapshotIsolation(this IMutableModel model)
            => model.SetOrRemoveAnnotation(SqlChangeTrackingAnnotationNames.SnapshotIsolation, true);

        public static bool IsChangeTrackingAutoCleanupEnabled(this IModel model)
            => model[SqlChangeTrackingAnnotationNames.AutoCleanup] as bool? ?? false;

        public static void SetChangeTrackingAutoCleanupEnabled(this IMutableModel model, bool enabled)
            => model.SetOrRemoveAnnotation(SqlChangeTrackingAnnotationNames.AutoCleanup, enabled);

        public static int GetChangeTrackingRetentionDays(this IModel model)
            => (int)model[SqlChangeTrackingAnnotationNames.ChangeRetentionDays];

        public static void SetChangeTrackingRetentionDays(this IMutableModel model, int days)
            => model.SetOrRemoveAnnotation(SqlChangeTrackingAnnotationNames.ChangeRetentionDays, days);

        public static IMutableEntityType SafeAddEntityType(this IMutableModel model, Type entityType)
        {
            var mutableEntityType = model.FindEntityType(entityType) ?? model.AddEntityType(entityType);

            return mutableEntityType;
        }
    }
}

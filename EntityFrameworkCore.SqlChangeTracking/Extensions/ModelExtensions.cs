using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class ModelExtensions
    {
        public static bool IsSnapshotIsolationEnabled(this IModel model)
            => model[SqlChangeTrackingAnnotationNames.SnapshotIsolation] as bool? ?? false;

        public static void SetSnapshotIsolationEnabled(this IMutableModel model, bool enabled = false)
            => model.SetOrRemoveAnnotation(SqlChangeTrackingAnnotationNames.SnapshotIsolation, enabled);

        public static bool GetChangeTrackingAutoCleanupEnabled(this IModel model)
            => model[SqlChangeTrackingAnnotationNames.AutoCleanup] as bool? ?? false;

        public static void SetChangeTrackingAutoCleanupEnabled(this IMutableModel model, bool enabled = false)
            => model.SetOrRemoveAnnotation(SqlChangeTrackingAnnotationNames.AutoCleanup, enabled);

        public static int GetChangeRetentionDays(this IModel model)
            => (int)model[SqlChangeTrackingAnnotationNames.ChangeRetentionDays];

        public static void SetChangeRetentionDays(this IMutableModel model, int days = 2)
            => model.SetOrRemoveAnnotation(SqlChangeTrackingAnnotationNames.ChangeRetentionDays, days);
    }
}

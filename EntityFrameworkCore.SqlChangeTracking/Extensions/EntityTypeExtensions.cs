using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;


namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class EntityTypeExtensions
    {
        public static bool IsSqlChangeTrackingEnabled(this IEntityType entityType)
            => entityType[SqlChangeTrackingAnnotationNames.Enabled] as bool? ?? false;

        public static void SetSqlChangeTrackingEnabled(this IMutableEntityType entityType, bool enabled = true)
            => entityType.SetOrRemoveAnnotation(SqlChangeTrackingAnnotationNames.Enabled, enabled);

        public static bool GetTrackColumns(this IEntityType entityType)
            => entityType[SqlChangeTrackingAnnotationNames.TrackColumns] as bool? ?? false;

        public static void SetTrackColumns(this IMutableEntityType entityType, bool enabled = true)
            => entityType.SetOrRemoveAnnotation(SqlChangeTrackingAnnotationNames.TrackColumns, enabled);

        public static string[] GetColumnNames(this IEntityType entityType)
        {
            return entityType.GetProperties().Select(p => p.GetColumnName()).ToArray();
        }
    }
}

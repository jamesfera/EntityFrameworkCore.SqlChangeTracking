using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EntityFrameworkCore.SqlChangeTracking.Extensions
{
    public static class MigrationExtensions
    {
        public static bool IsChangeTrackingEnabled(this AlterDatabaseOperation operation)
            => operation.FindAnnotation(SqlChangeTrackingAnnotationNames.Enabled)?.Value as bool? ?? false;

        public static int ChangeTrackingRetentionDays(this AlterDatabaseOperation operation)
        {
            return (int) operation.FindAnnotation(SqlChangeTrackingAnnotationNames.ChangeRetentionDays).Value;
        }
        public static bool ChangeTrackingAutoCleanUp(this AlterDatabaseOperation operation)
        {
            return (bool)operation.FindAnnotation(SqlChangeTrackingAnnotationNames.AutoCleanup).Value;
        }
        public static bool IsChangeTrackingEnabled(this TableOperation operation)
            => operation.FindAnnotation(SqlChangeTrackingAnnotationNames.Enabled)?.Value as bool? ?? false;

        public static bool ChangeTrackingTrackColumns(this TableOperation operation)
            => operation.FindAnnotation(SqlChangeTrackingAnnotationNames.TrackColumns).Value as bool? ?? false;
        
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EntityFrameworkCore.SqlChangeTracking.Extensions
{
    public static class MigrationBuilderExtensions
    {
        public static MigrationBuilder EnableChangeTrackingForTable(this MigrationBuilder migrationBuilder, string name, string? schema = null, bool trackColumns = false)
        {
            migrationBuilder.AlterTable(name: name, schema: schema)
                .Annotation("ChangeTracking", true)
                .Annotation(SqlChangeTrackingAnnotationNames.TrackColumns, trackColumns);

            return migrationBuilder;
        }

        public static MigrationBuilder DisableChangeTrackingForTable(this MigrationBuilder migrationBuilder, string name, string? schema = null)
        {
            migrationBuilder.AlterTable(name: name, schema: schema)
                .Annotation("ChangeTracking", false);

            return migrationBuilder;
        }

        public static MigrationBuilder EnableChangeTrackingForDatabase(this MigrationBuilder migrationBuilder, int changeRetentionDays = 2, bool autoCleanUp = true)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("ChangeTracking", true)
                .Annotation(SqlChangeTrackingAnnotationNames.ChangeRetentionDays, changeRetentionDays)
                .Annotation(SqlChangeTrackingAnnotationNames.AutoCleanup, autoCleanUp);

            return migrationBuilder;
        }

        public static MigrationBuilder DisableChangeTrackingForDatabase(this MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AlterDatabase().OldAnnotation("ChangeTracking", false);

            return migrationBuilder;
        }
    }
}

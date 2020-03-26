using System;
using System.Collections.Generic;
using System.Text;
using EntityFrameworkCore.SqlChangeTracking.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class MigrationBuilderExtensions
    {
        public static MigrationBuilder EnableChangeTrackingForTable(this MigrationBuilder migrationBuilder, string name, string? schema = null, bool trackColumns = false)
        {
            migrationBuilder.AlterTable(name: name, schema: schema)
                .Annotation(SqlChangeTrackingAnnotationNames.Enabled, true)
                .Annotation(SqlChangeTrackingAnnotationNames.TrackColumns, trackColumns);

            return migrationBuilder;
        }

        public static MigrationBuilder DisableChangeTrackingForTable(this MigrationBuilder migrationBuilder, string name, string? schema = null)
        {
            migrationBuilder.AlterTable(name: name, schema: schema)
                .OldAnnotation(SqlChangeTrackingAnnotationNames.Enabled, true);

            return migrationBuilder;
        }

        public static MigrationBuilder EnableChangeTrackingForDatabase(this MigrationBuilder migrationBuilder, int changeRetentionDays = 2, bool autoCleanUp = true)
        {
            migrationBuilder.Operations.Add(new EnableChangeTrackingForDatabaseOperation(changeRetentionDays, autoCleanUp));
            //migrationBuilder.AlterDatabase()
            //    .Annotation(SqlChangeTrackingAnnotationNames.Enabled, true)
            //    .Annotation(SqlChangeTrackingAnnotationNames.ChangeRetentionDays, changeRetentionDays)
            //    .Annotation(SqlChangeTrackingAnnotationNames.AutoCleanup, autoCleanUp);

            return migrationBuilder;
        }

        public static MigrationBuilder DisableChangeTrackingForDatabase(this MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .OldAnnotation(SqlChangeTrackingAnnotationNames.Enabled, true);

            return migrationBuilder;
        }
    }
}

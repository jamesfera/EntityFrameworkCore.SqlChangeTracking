using EntityFrameworkCore.SqlChangeTracking.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class MigrationBuilderExtensions
    {
        public static MigrationBuilder EnableChangeTrackingForTable(this MigrationBuilder migrationBuilder, string table, string? schema = null, bool trackColumns = false)
        {
            migrationBuilder.Operations.Add(new EnableChangeTrackingForTableOperation(table, schema, trackColumns));

            return migrationBuilder;
        }

        public static MigrationBuilder DisableChangeTrackingForTable(this MigrationBuilder migrationBuilder, string table, string? schema = null)
        {
            migrationBuilder.Operations.Add(new DisableChangeTrackingForTableOperation(table, schema));

            return migrationBuilder;
        }

        public static MigrationBuilder EnableChangeTrackingForDatabase(this MigrationBuilder migrationBuilder, int changeRetentionDays = 7, bool autoCleanUp = true)
        {
            migrationBuilder.Operations.Add(new EnableChangeTrackingForDatabaseOperation(changeRetentionDays, autoCleanUp));

            return migrationBuilder;
        }

        public static MigrationBuilder DisableChangeTrackingForDatabase(this MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Operations.Add(new DisableChangeTrackingForDatabaseOperation());

            return migrationBuilder;
        }
        public static MigrationBuilder SnapshotIsolation(this MigrationBuilder migrationBuilder, bool enabled)
        {
            migrationBuilder.Operations.Add(new SnapshotIsolationOperation(enabled));

            return migrationBuilder;
        }
    }
}

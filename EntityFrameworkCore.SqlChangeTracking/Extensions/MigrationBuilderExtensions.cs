using EntityFrameworkCore.SqlChangeTracking.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class MigrationBuilderExtensions
    {
        /// <param name="idempotent">When true, change tracking is only enabled if it is not already enabled for the table.</param>
        public static MigrationBuilder EnableChangeTrackingForTable(this MigrationBuilder migrationBuilder, string table, string? schema = null, bool trackColumns = false, bool idempotent = false)
        {
            migrationBuilder.Operations.Add(new EnableChangeTrackingForTableOperation(table, schema, trackColumns, idempotent));

            return migrationBuilder;
        }

        /// <param name="idempotent">When true, change tracking is only disabled if it is currently enabled for the table.</param>
        public static MigrationBuilder DisableChangeTrackingForTable(this MigrationBuilder migrationBuilder, string table, string? schema = null, bool idempotent = false)
        {
            migrationBuilder.Operations.Add(new DisableChangeTrackingForTableOperation(table, schema, idempotent));

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

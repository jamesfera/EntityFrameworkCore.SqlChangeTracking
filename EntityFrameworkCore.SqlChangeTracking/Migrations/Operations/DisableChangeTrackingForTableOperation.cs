using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EntityFrameworkCore.SqlChangeTracking.Migrations.Operations
{
    public class DisableChangeTrackingForTableOperation : MigrationOperation
    {
        public string Table { get; set; }
        public string? Schema { get; set; }

        /// <summary>
        /// When true, the generated SQL only disables change tracking if it is currently enabled for the table.
        /// </summary>
        public bool Idempotent { get; set; }

        public DisableChangeTrackingForTableOperation(string table, string? schema = null, bool idempotent = false)
        {
            Table = table;
            Schema = schema;
            Idempotent = idempotent;
        }
    }
}

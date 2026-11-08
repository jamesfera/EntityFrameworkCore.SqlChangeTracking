using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EntityFrameworkCore.SqlChangeTracking.Migrations.Operations
{
    public class EnableChangeTrackingForTableOperation : MigrationOperation
    {
        public string Table { get; set; }
        public string? Schema { get; set; }
        public bool TrackColumns { get; set; }

        /// <summary>
        /// When true, the generated SQL only enables change tracking if it is not already enabled for the table.
        /// </summary>
        public bool Idempotent { get; set; }

        public EnableChangeTrackingForTableOperation(string table, string? schema = null, bool trackColumns = false, bool idempotent = false)
        {
            Table = table;
            Schema = schema;
            TrackColumns = trackColumns;
            Idempotent = idempotent;
        }
    }
}

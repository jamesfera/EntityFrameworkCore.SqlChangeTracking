using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EntityFrameworkCore.SqlChangeTracking.Migrations.Operations
{
    public class EnableChangeTrackingForTableOperation : MigrationOperation
    {
        public string Table { get; set; }
        public string? Schema { get; set; }
        public bool TrackColumns { get; set; }

        public EnableChangeTrackingForTableOperation(string table, string? schema = null, bool trackColumns = false)
        {
            Table = table;
            Schema = schema;
            TrackColumns = trackColumns;
        }
    }
}

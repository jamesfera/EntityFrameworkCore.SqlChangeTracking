using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EntityFrameworkCore.SqlChangeTracking.Migrations.Operations
{
    public class DisableChangeTrackingForTableOperation : MigrationOperation
    {
        public string Table { get; set; }
        public string? Schema { get; set; }

        public DisableChangeTrackingForTableOperation(string table, string? schema = null)
        {
            Table = table;
            Schema = schema;
        }
    }
}

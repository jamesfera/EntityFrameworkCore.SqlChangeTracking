using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EntityFrameworkCore.SqlChangeTracking.Migrations.Operations
{
    public class SnapshotIsolationOperation(bool enabled) : MigrationOperation
    {
        public bool Enabled { get; } = enabled;
    }
}

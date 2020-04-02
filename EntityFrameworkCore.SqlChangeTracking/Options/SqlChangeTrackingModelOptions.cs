namespace EntityFrameworkCore.SqlChangeTracking.Options
{
    public class SqlChangeTrackingModelOptions
    {
        public bool EnableSnapshotIsolation { get; set; } = true;
        public int RetentionDays { get; set; } = 2;
        public bool AutoCleanUp { get; set; } = true;
    }
}
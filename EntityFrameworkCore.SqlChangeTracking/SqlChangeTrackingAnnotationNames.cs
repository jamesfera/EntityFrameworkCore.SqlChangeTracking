namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class SqlChangeTrackingAnnotationNames
    {
        public const string Prefix = "ChangeTracking:";
        public const string Enabled = Prefix + "Enabled";
        public const string SnapshotIsolation = Prefix + "SnapshotIsolation";
        public const string AutoCleanup = Prefix + "AutoCleanup";
        public const string ChangeRetentionDays = Prefix + "ChangeRetentionDays";
        public const string TrackColumns = Prefix + "TrackColumns";
    }
}

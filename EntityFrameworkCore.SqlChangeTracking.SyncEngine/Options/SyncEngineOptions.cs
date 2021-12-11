namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine.Options
{
    public class SyncEngineOptions
    {
        public string SyncContext { get; set; } = "Default";
        public bool MarkEntitiesAsSyncedOnInitialization { get; set; } = false;
        public bool ThrowOnStartupException { get; set; } = false;
    }
}

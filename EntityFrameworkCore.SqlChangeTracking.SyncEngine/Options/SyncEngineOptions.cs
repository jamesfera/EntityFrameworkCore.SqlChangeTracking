namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine.Options
{
    public class SyncEngineOptions
    {
        public string SyncContext { get; set; } = "Default";
        public bool SynchronizeChangesOnStartup { get; set; } = true;
        public bool ThrowOnStartupException { get; set; } = false;
    }
}

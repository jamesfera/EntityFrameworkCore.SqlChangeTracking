using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine
{
    public interface ISyncEngine
    {
        string SyncContext { get; }
        Type DbContextType { get; }

        public IReadOnlyList<IEntityType> SyncEntityTypes { get; }

        Task ProcessAllChanges(CancellationToken cancellationToken);

        Task ProcessChanges(IEntityType entityType, CancellationToken cancellationToken);
        Task ProcessChanges(string entityTypeName, CancellationToken cancellationToken);

        Task ProcessDataSet(IEntityType entityType, bool markSynced, string? primaryKeyStart, CancellationToken cancellationToken, Func<DataSetBatchProcessed, Task>? batchProcessedAction = null);
        Task ProcessDataSet(string entityTypeName, bool markSynced, string? primaryKeyStart, CancellationToken cancellationToken, Func<DataSetBatchProcessed, Task>? batchProcessedAction = null);

        Task SetChangeVersion(string entityName, long changeVersion);
        Task SetChangeVersion(IEntityType entityType, long changeVersion);

        Task MarkEntityAsSynced(string entityName);
        Task MarkEntityAsSynced(IEntityType entityType);

        Task Start(CancellationToken cancellationToken);
        Task Stop(CancellationToken cancellationToken);
    }
}

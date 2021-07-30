using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkCore.SqlChangeTracking.SyncEngine.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine
{
    public interface ISyncEngine
    {
        string SyncContext { get; }
        Type DbContextType { get; }
        Task ProcessAllChanges();
        Task ProcessChanges(IEntityType entityType);
        Task ProcessChanges<TEntity>();
        Task ProcessChanges(Type clrEntityType);

        Task ResetAllSyncVersions();
        Task MarkAllEntitiesAsSynced();

        //Task SetChangeVersion(IEntityType entityType, long changeVersion);

        Task MarkEntityAsSynced(IEntityType entityType);
        Task ResetSyncVersionForEntity(IEntityType entityType);

        Task Start(CancellationToken cancellationToken);
        Task Stop(CancellationToken cancellationToken);
    }
}

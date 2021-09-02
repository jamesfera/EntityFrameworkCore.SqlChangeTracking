using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        Task ProcessDataSet(IEntityType entityType, CancellationToken cancellationToken);
        Task ProcessDataSet(string entityTypeName, CancellationToken cancellationToken);

        Task SetChangeVersion(string entityName, long changeVersion);
        Task SetChangeVersion(IEntityType entityType, long changeVersion);

        Task MarkEntityAsSynced(string entityName);
        Task MarkEntityAsSynced(IEntityType entityType);

        Task Start(CancellationToken cancellationToken);
        Task Stop(CancellationToken cancellationToken);
    }
}

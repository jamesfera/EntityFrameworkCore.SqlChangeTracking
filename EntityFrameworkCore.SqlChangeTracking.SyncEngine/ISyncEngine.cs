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

        Task ProcessAllChanges();

        Task ProcessChanges(IEntityType entityType);
        Task ProcessChanges(string entityTypeName);

        Task ProcessDataSet(IEntityType entityType);
        Task ProcessDataSet(string entityTypeName);

        Task SetChangeVersion(string entityName, long changeVersion);
        Task SetChangeVersion(IEntityType entityType, long changeVersion);

        Task MarkEntityAsSynced(string entityName);
        Task MarkEntityAsSynced(IEntityType entityType);

        Task Start(CancellationToken cancellationToken);
        Task Stop(CancellationToken cancellationToken);
    }
}

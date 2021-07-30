using System.Collections.Generic;
using System.Threading.Tasks;
using EntityFrameworkCore.SqlChangeTracking.Models;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine
{
    public interface IChangeSetBatchProcessor<in TEntity, TContext> where TContext : DbContext 
    {
        Task ProcessBatch(IEnumerable<IChangeTrackingEntry<TEntity>> changes, IChangeSetProcessorContext<TContext> context);
    }
}
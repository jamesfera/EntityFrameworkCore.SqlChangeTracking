using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine
{
    public interface IChangeSetBatchProcessorFactory<TContext> where TContext : DbContext
    {
        IEnumerable<IChangeSetBatchProcessor<TEntity, TContext>> GetBatchProcessors<TEntity>(string syncContext);
    }
}
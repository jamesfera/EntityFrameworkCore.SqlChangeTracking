using System;
using System.Collections.Generic;
using System.Text;
using EntityFrameworkCore.SqlChangeTracking.Models;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine
{
    public enum ChangeBatchType { Changes, DataSet }

    public interface IChangeBatch<out TEntity>
    {
        IEnumerable<IChangeTrackingEntry<TEntity>> ChangeSet { get; }
        ChangeBatchType ChangeBatchType { get; }
    }

    public class ChangeBatch<TEntity> : IChangeBatch<TEntity>
    {
        public ChangeBatch(IEnumerable<IChangeTrackingEntry<TEntity>> changeSet, ChangeBatchType changeBatchType)
        {
            ChangeSet = changeSet;
            ChangeBatchType = changeBatchType;
        }

        public IEnumerable<IChangeTrackingEntry<TEntity>> ChangeSet { get; }
        public ChangeBatchType ChangeBatchType { get; }
    }
}

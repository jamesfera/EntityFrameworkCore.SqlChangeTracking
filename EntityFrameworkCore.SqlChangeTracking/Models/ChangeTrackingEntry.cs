using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkCore.SqlChangeTracking.Models
{
    public enum ChangeOperation { None, Insert, Update, Delete }
    public class ChangeTrackingEntry<T> : ChangeTrackingEntry
    {
        public T Entity { get; }

        public ChangeTrackingEntry(T entity,
            long? changeVersion,
            long? creationVersion,
            ChangeOperation? changeOperation,
            string? changeContext) 
            : base(changeVersion, creationVersion, changeOperation, changeContext)
        {
            Entity = entity;
        }

        public ChangeTrackingEntry<TNew> WithType<TNew>()
        {
            if (Entity is TNew newEntity)
                return new ChangeTrackingEntry<TNew>(newEntity, ChangeVersion, CreationVersion, ChangeOperation, ChangeContext);

            throw new InvalidOperationException($"Type: {typeof(T).PrettyName()} cannot be converted to Type: {typeof(TNew).PrettyName()}");
        }
    }

    public abstract class ChangeTrackingEntry
    {
        protected ChangeTrackingEntry(long? changeVersion, long? creationVersion, ChangeOperation? changeOperation, string? changeContext)
        {
            ChangeVersion = changeVersion;
            CreationVersion = creationVersion;
            ChangeOperation = changeOperation;
            ChangeContext = changeContext;
        }

        //[Column("SYS_CHANGE_VERSION")]
        public long? ChangeVersion { get; }
        //[Column("SYS_CHANGE_CREATION_VERSION")]
        public long? CreationVersion { get; }
        //[Column("SYS_CHANGE_OPERATION")]
        public ChangeOperation? ChangeOperation { get; }
        //[Column("SYS_CHANGE_CONTEXT")]
        public string? ChangeContext { get; }
    }
}

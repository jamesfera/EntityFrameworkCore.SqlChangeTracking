using System;

namespace EntityFrameworkCore.SqlChangeTracking.Models
{
    public enum ChangeOperation { None, Insert, Update, Delete }

    public interface IChangeTrackingEntry
    {
        long? ChangeVersion { get; }
        long? CreationVersion { get; }
        ChangeOperation ChangeOperation { get; }
        string? ChangeContext { get; }
        string TableName { get; }
    }

    //public interface IChangeTrackingEntry1<out T> : IChangeTrackingEntry
    //{
    //    T Entry { get; }
    //}

    public interface IChangeTrackingEntry<out T> : IChangeTrackingEntry
    {
        object PrimaryKey { get; }
        Type EntityType { get; }
    }

    public class ChangeTrackingEntry<T> : ChangeTrackingEntry, IChangeTrackingEntry<T>
    {
        public object PrimaryKey { get; }
        public Type EntityType { get; } = typeof(T);

        internal ChangeTrackingEntry(object primaryKey,
            long? changeVersion,
            long? creationVersion,
            ChangeOperation changeOperation,
            string? changeContext,
            string tableName)
            : base(changeVersion, creationVersion, changeOperation, changeContext, tableName)
        {
            PrimaryKey = primaryKey;
        }
    }

    //public class ChangeTrackingEntry1<T> : ChangeTrackingEntry, IChangeTrackingEntry1<T>
    //{
    //    public T Entry { get; }

    //    internal ChangeTrackingEntry1(T entity,
    //        long? changeVersion,
    //        long? creationVersion,
    //        ChangeOperation changeOperation,
    //        string? changeContext,
    //        string tableName) 
    //        : base(changeVersion, creationVersion, changeOperation, changeContext, tableName)
    //    {
    //        Entry = entity;
    //    }

    //    public ChangeTrackingEntry<TNew> WithType<TNew>()
    //    {
    //        if (Entry is TNew newEntity)
    //            return new ChangeTrackingEntry<TNew>(newEntity, ChangeVersion, CreationVersion, ChangeOperation, ChangeContext, TableName);

    //        throw new InvalidOperationException($"Type: {typeof(T).PrettyName()} cannot be converted to Type: {typeof(TNew).PrettyName()}");
    //    }
    //}

    public abstract class ChangeTrackingEntry : IChangeTrackingEntry
    {
        protected ChangeTrackingEntry(long? changeVersion, long? creationVersion, ChangeOperation changeOperation, string? changeContext, string tableName)
        {
            ChangeVersion = changeVersion;
            CreationVersion = creationVersion;
            ChangeOperation = changeOperation;
            ChangeContext = changeContext;
            TableName = tableName;
        }

        public long? ChangeVersion { get; }

        public long? CreationVersion { get; }

        public ChangeOperation ChangeOperation { get; }

        public string? ChangeContext { get; }

        public string TableName { get; }
    }
}

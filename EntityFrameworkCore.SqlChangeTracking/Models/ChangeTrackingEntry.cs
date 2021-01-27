using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkCore.SqlChangeTracking.Models
{
    public enum ChangeOperation { None, Insert, Update, Delete }

    public interface IChangeTrackingEntry
    {
        long? ChangeVersion { get; }
        long? CreationVersion { get; }
        ChangeOperation ChangeOperation { get; }
        string? ChangeContext { get; }
    }

    public interface IChangeTrackingEntry<out T> : IChangeTrackingEntry
    {
        T Entry { get; }
    }

    public class ChangeTrackingEntry<T> : ChangeTrackingEntry, IChangeTrackingEntry<T>
    {
        public T Entry { get; }

        internal ChangeTrackingEntry(T entity,
            long? changeVersion,
            long? creationVersion,
            ChangeOperation changeOperation,
            string? changeContext) 
            : base(changeVersion, creationVersion, changeOperation, changeContext)
        {
            Entry = entity;
        }

        public ChangeTrackingEntry<TNew> WithType<TNew>()
        {
            if (Entry is TNew newEntity)
                return new ChangeTrackingEntry<TNew>(newEntity, ChangeVersion, CreationVersion, ChangeOperation, ChangeContext);

            throw new InvalidOperationException($"Type: {typeof(T).PrettyName()} cannot be converted to Type: {typeof(TNew).PrettyName()}");
        }
    }

    public abstract class ChangeTrackingEntry : IChangeTrackingEntry
    {
        protected ChangeTrackingEntry(long? changeVersion, long? creationVersion, ChangeOperation changeOperation, string? changeContext)
        {
            ChangeVersion = changeVersion;
            CreationVersion = creationVersion;
            ChangeOperation = changeOperation;
            ChangeContext = changeContext;
        }

        public long? ChangeVersion { get; }

        public long? CreationVersion { get; }

        public ChangeOperation ChangeOperation { get; }

        public string? ChangeContext { get; }
    }
}

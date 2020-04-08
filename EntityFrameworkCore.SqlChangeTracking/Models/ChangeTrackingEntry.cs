using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkCore.SqlChangeTracking.Models
{
    public enum ChangeOperation { None, Insert, Update, Delete }

    public interface IChangeTrackingEntry<out T>
    {
        long? ChangeVersion { get; }
        long? CreationVersion { get; }
        ChangeOperation ChangeOperation { get; }
        string? ChangeContext { get; }

        T Entity { get; }
    }

    public class ChangeTrackingEntry<T> : ChangeTrackingEntry, IChangeTrackingEntry<T>
    {
        public T Entity { get; }

        internal ChangeTrackingEntry(T entity,
            long? changeVersion,
            long? creationVersion,
            ChangeOperation changeOperation,
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

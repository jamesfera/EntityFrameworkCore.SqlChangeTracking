using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkCore.SqlChangeTracking.Models
{
    public enum ChangeOperation { Insert, Update, Delete }
    public class ChangeTrackingEntry<T> : ChangeTrackingEntry
    {
        public T Entity { get; }

        internal ChangeTrackingEntry(T entity) => Entity = entity;
    }

    public class ChangeTrackingEntry
    {
        //[Column("SYS_CHANGE_VERSION")]
        public long ChangeVersion { get; set; }
        //[Column("SYS_CHANGE_CREATION_VERSION")]
        public long? CreationVersion { get; set; }
        //[Column("SYS_CHANGE_OPERATION")]
        public ChangeOperation ChangeOperation { get; set; }
        //[Column("SYS_CHANGE_CONTEXT")]
        public string? ChangeContext { get; set; }
    }
}

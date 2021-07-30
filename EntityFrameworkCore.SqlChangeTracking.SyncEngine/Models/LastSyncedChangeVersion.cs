using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine.Models
{
    public class LastSyncedChangeVersion : IEntityTypeConfiguration<LastSyncedChangeVersion>
    {
        public string TableName { get; set; }
        public string SyncContext { get; set; }
        public long LastSyncedVersion { get; set; }

        //public LastSyncedChangeVersion(string tableName, long lastSyncedVersion)
        //{
        //    TableName = tableName;
        //    LastSyncedVersion = lastSyncedVersion;
        //}

        public void Configure(EntityTypeBuilder<LastSyncedChangeVersion> builder)
        {
            builder.HasKey(e => new { e.TableName, e.SyncContext });

            builder.Property(e => e.TableName).HasMaxLength(100);
            builder.Property(e => e.SyncContext).HasMaxLength(100);
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine
{
    public static class EntityTypeBuilderExtensions
    {
        public static EntityTypeBuilder<TEntity> WithSyncEngine<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder, string syncContext = "Default") where TEntity : class
            => (EntityTypeBuilder<TEntity>)WithSyncEngine((EntityTypeBuilder)entityTypeBuilder, syncContext);

        public static EntityTypeBuilder WithSyncEngine(this EntityTypeBuilder entityTypeBuilder, string syncContext = "Default")
        {
            entityTypeBuilder.WithSqlChangeTracking();

            entityTypeBuilder.Metadata.EnableSyncEngine(syncContext);

            entityTypeBuilder.ToTable(t => t.HasTrigger("sync_trigger"));
           
            return entityTypeBuilder;
        }
    }
}
﻿using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class EntityTypeBuilderExtensions
    {
        public static EntityTypeBuilder<TEntity> WithSqlChangeTracking<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder, bool trackColumns = false) where TEntity : class
            => (EntityTypeBuilder<TEntity>)WithSqlChangeTracking((EntityTypeBuilder)entityTypeBuilder, trackColumns);

        public static EntityTypeBuilder WithSqlChangeTracking(this EntityTypeBuilder entityTypeBuilder, bool trackColumns = false)
        {
            //entityTypeBuilder.Metadata.Model.SetSnapshotIsolationEnabled(true);

            entityTypeBuilder.Metadata.EnableSqlChangeTracking();
            entityTypeBuilder.Metadata.SetTrackColumns(trackColumns);

            return entityTypeBuilder;
        }
    }
}

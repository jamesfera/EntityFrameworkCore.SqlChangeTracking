using System;
using System.Collections.Generic;
using System.Text;
using EntityFrameworkCore.SqlChangeTracking.Models;
using EntityFrameworkCore.SqlChangeTracking.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkCore.SqlChangeTracking.Extensions.Internal
{
    internal static class InternalDbContextExtensions
    {
        public static IAsyncEnumerable<IChangeTrackingEntry<T>> Next<T>(this DbContext context, IEntityType entityType, long version) where T : class, new()
        {
            Validate(entityType);

            var sql = ChangeTableSqlStatements.GetNextChangeSetExpression(entityType, version.ToString());

            return new AsyncEnumerableWrapper<T>(context.ToChangeSet<T>(sql), sql);
        }

        public static IAsyncEnumerable<IChangeTrackingEntry<T>> All<T>(this DbContext context, IEntityType entityType, long version) where T : class, new()
        {
            Validate(entityType);

            var sql = ChangeTableSqlStatements.GetAllChangeSetsExpression(entityType, true, version);

            return new AsyncEnumerableWrapper<T>(context.ToChangeSet<T>(sql), sql);
        }

        internal static void Validate(IEntityType entityType)
        {
            var changeTrackingEnabled = entityType.IsSqlChangeTrackingEnabled();

            if (!changeTrackingEnabled)
                throw new ArgumentException($"Change tracking is not enabled for Entity: '{entityType.Name}'. Call '.{nameof(EntityTypeBuilderExtensions.WithSqlChangeTracking)}()' at Entity build time.");
        }
    }
}

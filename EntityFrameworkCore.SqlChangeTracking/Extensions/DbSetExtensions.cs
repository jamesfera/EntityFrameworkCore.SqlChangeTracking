using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class DbSetExtensions
    {
        public static IQueryable<T> GetChangesSinceVersion<T>(this DbSet<T> dbSet, long version) where T : class
        {
            //TODO Handle deletes

            var context = dbSet.GetService<ICurrentDbContext>().Context;

            var entityType = context.Model.FindEntityType(typeof(T));

            var tableName = entityType.GetTableName();

            var primaryKey = entityType.FindPrimaryKey();

            var primaryKeyColumn = primaryKey.Properties.Select(p => p.GetColumnName()).First();
            
            var prefixedColumnNames = string.Join(",", entityType.GetColumnNames().Select(c => $"T.{c}"));

            return dbSet.FromSqlRaw($"SELECT {prefixedColumnNames} FROM {tableName} AS T RIGHT OUTER JOIN CHANGETABLE(CHANGES {tableName}, {version}) AS CT ON T.{primaryKeyColumn} = CT.{primaryKeyColumn}").AsNoTracking();
        }

        public static string[] GetColumnNames(this IEntityType entityType)
        {
            return entityType.GetProperties().Select(p => p.GetColumnName()).ToArray();
        }
    }
}

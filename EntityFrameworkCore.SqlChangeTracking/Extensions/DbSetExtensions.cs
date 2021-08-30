using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkCore.SqlChangeTracking.Extensions.Internal;
using EntityFrameworkCore.SqlChangeTracking.Models;
using EntityFrameworkCore.SqlChangeTracking.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.SqlChangeTracking.Extensions
{
    public static class DbSetExtensions
    {
        //public interface ICurrentTrackingContext
        //{
        //    string GetTrackingContextForTable(string tableName);
        //}

        //public interface ICurrentContextSetter
        //{
        //    void SetContextForTable(string tableName, string trackingContext);
        //}

        //public class CurrentTrackingContext : ICurrentTrackingContext, ICurrentContextSetter
        //{
        //    ConcurrentDictionary<string, string> _contextCache = new ConcurrentDictionary<string, string>();

        //    public string GetTrackingContextForTable(string tableName)
        //    {
        //        _contextCache.TryGetValue(tableName, out string trackingContext);

        //        return trackingContext;
        //    }

        //    public void SetContextForTable(string tableName, string trackingContext)
        //    {
        //        _contextCache.TryAdd(tableName, trackingContext);
        //    }
        //}

        public static Task ResetChangeTracking<T>(this DbSet<T> dbSet) where T : class
        {
            var context = dbSet.GetService<ICurrentDbContext>().Context;

            var entityType = context.Model.FindEntityType(typeof(T));

            return context.ResetChangeTracking(entityType);
        }

        public static IDisposable WithTrackingContext<T>(this DbSet<T> dbSet, string trackingContext) where T : class
        {
            var context = dbSet.GetService<ICurrentDbContext>().Context;
            
            var tableName = context.Model.FindEntityType(typeof(T)).GetTableName();

            return new TrackingContextAsyncLocalCache.ChangeTrackingContext(tableName, trackingContext);
        }

        public static IChangesQueryContext<T> Changes<T>(this DbSet<T> dbSet) where T : class, new()
        {
            var context = dbSet.GetService<ICurrentDbContext>().Context;

            var entityType = context.Model.FindEntityType(typeof(T));

            var changeTrackingEnabled = entityType.IsSqlChangeTrackingEnabled();

            if (!changeTrackingEnabled)
                throw new ArgumentException($"Change tracking is not enabled for Entity: '{entityType.ClrType}'. Call '.{nameof(EntityTypeBuilderExtensions.WithSqlChangeTracking)}()' at Entity build time.");
            
            return new ChangesQueryContext<T>(dbSet);
        }

        public static string ToSql<TEntity>(this IQueryable<TEntity> query) where TEntity : class
        {
            var enumerator = query.Provider.Execute<IEnumerable<TEntity>>(query.Expression).GetEnumerator();
            var relationalCommandCache = enumerator.Private("_relationalCommandCache");
            var selectExpression = relationalCommandCache.Private<SelectExpression>("_selectExpression");
            var factory = relationalCommandCache.Private<IQuerySqlGeneratorFactory>("_querySqlGeneratorFactory");

            var sqlGenerator = factory.Create();
            var command = sqlGenerator.GetCommand(selectExpression);

            string sql = command.CommandText;
            return sql;
        }

        private static object Private(this object obj, string privateField) => obj?.GetType().GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);
        private static T Private<T>(this object obj, string privateField) => (T)obj?.GetType().GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);

        
    }

    
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using EntityFrameworkCore.SqlChangeTracking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class DbSetExtensions
    {
        private const string EntityTablePrefix = "T";
        private const string ChangeTablePrefix = "CT";

        public interface ICurrentTrackingContext
        {
            string GetTrackingContextForTable(string tableName);
        }

        public interface ICurrentContextSetter
        {
            void SetContextForTable(string tableName, string trackingContext);
        }

        public class CurrentTrackingContext : ICurrentTrackingContext, ICurrentContextSetter
        {
            ConcurrentDictionary<string, string> _contextCache = new ConcurrentDictionary<string, string>();

            public string GetTrackingContextForTable(string tableName)
            {
                _contextCache.TryGetValue(tableName, out string trackingContext);

                return trackingContext;
            }

            public void SetContextForTable(string tableName, string trackingContext)
            {
                _contextCache.TryAdd(tableName, trackingContext);
            }
        }

        public static IDisposable WithTrackingContext<T>(this DbSet<T> dbSet, string trackingContext) where T : class
        {
            var context = dbSet.GetService<ICurrentDbContext>().Context;
            
            var tableName = context.Model.FindEntityType(typeof(T)).GetTableName();

            return new TrackingContextAsyncLocalCache.ChangeTrackingContext(tableName, trackingContext);
        }

        internal static IEnumerable<ChangeTrackingEntry<T>> GetChangesSinceVersion<T>(this DbContext context, IEntityType entityType, long version) where T : class, new()
        {
            //TODO Handle deletes

            Validate(entityType);

            var tableName = entityType.GetTableName();

            var primaryKey = entityType.FindPrimaryKey();

            var prefixedColumnNames = string.Join(",", entityType.GetColumnNames().Select(c => $"{EntityTablePrefix}.{c}"));

            prefixedColumnNames += ",SYS_CHANGE_VERSION as ChangeVersion, SYS_CHANGE_CREATION_VERSION as CreationVersion, SYS_CHANGE_OPERATION as ChangeOperation, SYS_CHANGE_CONTEXT as ChangeContext";

            var pks = primaryKey.Properties.Select(pk => $"{EntityTablePrefix}.{pk.GetColumnName()} = {ChangeTablePrefix}.{pk.GetColumnName()}");

            var joinKeyStatement = string.Join(" AND ", pks);

            var sqlBuilder = new StringBuilder();

            if (context.Model.IsSnapshotIsolationEnabled())
                sqlBuilder.AppendLine("SET TRANSACTION ISOLATION LEVEL SNAPSHOT;");

            sqlBuilder.AppendLine("BEGIN TRAN");

            sqlBuilder.AppendLine($"SELECT {prefixedColumnNames} FROM {tableName} AS {EntityTablePrefix} RIGHT OUTER JOIN CHANGETABLE(CHANGES {tableName}, {version}) AS {ChangeTablePrefix} ON {joinKeyStatement} ORDER BY ChangeVersion");

            sqlBuilder.AppendLine("COMMIT TRAN");

            var reader = context.Database.ExecuteSqlQuery(sqlBuilder.ToString()).DbDataReader;

            while (reader.Read())
                yield return mapToChangeTrackingEntry<T>(reader, entityType);
        }

        public static IEnumerable<ChangeTrackingEntry<T>> GetChangesSinceVersion<T>(this DbSet<T> dbSet, long version) where T : class, new()
        {
            var context = dbSet.GetService<ICurrentDbContext>().Context;

            var entityType = context.Model.FindEntityType(typeof(T));
        
            return GetChangesSinceVersion<T>(context, entityType, version);
        }

        public static IEnumerable<ChangeTrackingEntry<T>> GetAllChanges<T>(this DbSet<T> dbSet, long version) where T : class, new()
        {
            //TODO Handle deletes

            var context = dbSet.GetService<ICurrentDbContext>().Context;

            var entityType = context.Model.FindEntityType(typeof(T));

            Validate(entityType);

            var tableName = entityType.GetTableName();

            var primaryKey = entityType.FindPrimaryKey();

            var prefixedColumnNames = string.Join(",", entityType.GetColumnNames().Select(c => $"{EntityTablePrefix}.{c}"));

            prefixedColumnNames += ",SYS_CHANGE_VERSION as ChangeVersion, SYS_CHANGE_CREATION_VERSION as CreationVersion, SYS_CHANGE_OPERATION as ChangeOperation, SYS_CHANGE_CONTEXT as ChangeContext";

            var pks = primaryKey.Properties.Select(pk => $"{EntityTablePrefix}.{pk.GetColumnName()} = {ChangeTablePrefix}.{pk.GetColumnName()}");

            var joinKeyStatement = string.Join(" AND ", pks);

            var sqlBuilder = new StringBuilder();

            if (context.Model.IsSnapshotIsolationEnabled())
                sqlBuilder.AppendLine("SET TRANSACTION ISOLATION LEVEL SNAPSHOT;");

            sqlBuilder.AppendLine("BEGIN TRAN");

            sqlBuilder.AppendLine($"SELECT {prefixedColumnNames} FROM {tableName} AS {EntityTablePrefix} LEFT OUTER JOIN CHANGETABLE(CHANGES {tableName}, null) AS {ChangeTablePrefix} ON {joinKeyStatement}");

            sqlBuilder.AppendLine("COMMIT TRAN");

            var reader = context.Database.ExecuteSqlQuery(sqlBuilder.ToString()).DbDataReader;

            while (reader.Read())
                yield return mapToChangeTrackingEntry<T>(reader, entityType);
        }

        private static ChangeTrackingEntry<T> mapToChangeTrackingEntry<T>(DbDataReader reader, IEntityType entityType) where T : class, new()
        {
            var entity = new T();
            var entry = new ChangeTrackingEntry<T>(entity);

            var byteArray = reader[nameof(ChangeTrackingEntry<T>.ChangeContext)] as byte[];

            entry.ChangeContext = byteArray == null ? null : Encoding.UTF8.GetString(byteArray);
            entry.ChangeVersion = reader[nameof(ChangeTrackingEntry<T>.ChangeVersion)] as long?;
            entry.CreationVersion = reader[nameof(ChangeTrackingEntry<T>.CreationVersion)] as long?;

            var operation = reader[nameof(ChangeTrackingEntry<T>.ChangeOperation)] as string;

            entry.ChangeOperation = operation switch
                {
                    "I" => ChangeOperation.Insert,
                    "U" => ChangeOperation.Update,
                    "D" => ChangeOperation.Delete,
                    _ => ChangeOperation.None
                };

            foreach (var propertyInfo in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var columnName = entityType.GetProperties().First(p => p.Name == propertyInfo.Name).GetColumnName();

                object? readerValue = reader[columnName];

                readerValue = readerValue == DBNull.Value ? null : readerValue;

                propertyInfo.SetValue(entity, readerValue);
            }

            return entry;
        }

        private static void Validate(IEntityType entityType)
        {
            var changeTrackingEnabled = entityType.IsSqlChangeTrackingEnabled();

            if(!changeTrackingEnabled)
                throw new ArgumentException($"Change tracking is not enabled for Entity: '{entityType.Name}'. Call '.{nameof(EntityTypeBuilderExtensions.WithSqlChangeTracking)}()' at Entity build time.");
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

        internal static RelationalDataReader ExecuteSqlQuery(this DatabaseFacade databaseFacade, string sql, params object[] parameters)
        {
            var concurrencyDetector = databaseFacade.GetService<IConcurrencyDetector>();

            using var cd = concurrencyDetector.EnterCriticalSection();
            
            var rawSqlCommand = databaseFacade
                .GetService<IRawSqlCommandBuilder>()
                .Build(sql, parameters);

            var paramObject = new RelationalCommandParameterObject(databaseFacade.GetService<IRelationalConnection>(), rawSqlCommand.ParameterValues, null, null);

            return rawSqlCommand
                .RelationalCommand
                .ExecuteReader(paramObject);
        }
    }

    
}

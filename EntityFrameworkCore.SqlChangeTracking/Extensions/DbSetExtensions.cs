using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EntityFrameworkCore.SqlChangeTracking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class DbSetExtensions
    {
        private const string EntityTablePrefix = "T";
        private const string ChangeTablePrefix = "CT";

        public static IEnumerable<ChangeTrackingEntry<T>> GetChangesSinceVersion<T>(this DbSet<T> dbSet, long version) where T : class, new()
        {
            //TODO Handle deletes

            Validate(dbSet);

            var context = dbSet.GetService<ICurrentDbContext>().Context;

            var entityType = context.Model.FindEntityType(typeof(T));

            var tableName = entityType.GetTableName();

            var primaryKey = entityType.FindPrimaryKey();

            //var primaryKeyColumn = primaryKey.Properties.Select(p => p.GetColumnName()).First();
            
            var prefixedColumnNames = string.Join(",", entityType.GetColumnNames().Select(c => $"{EntityTablePrefix}.{c}"));

            prefixedColumnNames += ",SYS_CHANGE_VERSION as ChangeVersion, SYS_CHANGE_CREATION_VERSION as CreationVersion, SYS_CHANGE_OPERATION as ChangeOperation, SYS_CHANGE_CONTEXT as ChangeContext";

            var pks = primaryKey.Properties.Select(pk => $"{EntityTablePrefix}.{pk.GetColumnName()} = {ChangeTablePrefix}.{pk.GetColumnName()}");

            var joinKeyStatement = string.Join(" AND ", pks);

            //SET TRANSACTION ISOLATION LEVEL SNAPSHOT;
            //BEGIN TRAN

            var q = $"SELECT {prefixedColumnNames} FROM {tableName} AS {EntityTablePrefix} RIGHT OUTER JOIN CHANGETABLE(CHANGES {tableName}, {version}) AS {ChangeTablePrefix} ON {joinKeyStatement}";

            var reader = context.Database.ExecuteSqlQuery(q).DbDataReader;

            List<ChangeTrackingEntry<T>> results = new List<ChangeTrackingEntry<T>>();

            while (reader.Read())
            {
                results.Add(mapToChangeTrackingEntry<T>(reader, entityType));
            }

            return results.AsEnumerable();
        }

        private static ChangeTrackingEntry<T> mapToChangeTrackingEntry<T>(DbDataReader reader, IEntityType entityType) where T : class, new()
        {
            var entity = new T();
            var entry = new ChangeTrackingEntry<T>(entity);

            entry.ChangeContext = reader[nameof(ChangeTrackingEntry<T>.ChangeContext)] as string;
            entry.ChangeVersion = (long)reader[nameof(ChangeTrackingEntry<T>.ChangeVersion)];
            entry.CreationVersion = reader[nameof(ChangeTrackingEntry<T>.CreationVersion)] as long?;

            var operation = (string) reader[nameof(ChangeTrackingEntry<T>.ChangeOperation)];

            entry.ChangeOperation = operation switch
                {
                    "I" => ChangeOperation.Insert,
                    "U" => ChangeOperation.Update,
                    "D" => ChangeOperation.Delete,
                    _ => throw new Exception()
                };

            foreach (var propertyInfo in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var columnName = entityType.GetProperties().First(p => p.Name == propertyInfo.Name).GetColumnName();

                propertyInfo.SetValue(entity, reader[columnName]);
            }

            return entry;
        }

        private static void Validate<T>(DbSet<T> dbSet) where T : class
        {
            var context = dbSet.GetService<ICurrentDbContext>().Context;

            var entityType = context.Model.FindEntityType(typeof(T));
            
            var changeTrackingEnabled = entityType.IsSqlChangeTrackingEnabled();

            if(!changeTrackingEnabled)
                throw new ArgumentException($"Change tracking is not enabled for Entity: '{entityType.Name}'. Call '.WithSqlChangeTracking()' at Entity build time.");
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

        public static RelationalDataReader ExecuteSqlQuery(this DatabaseFacade databaseFacade, string sql, params object[] parameters)
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

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EntityFrameworkCore.SqlChangeTracking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EntityFrameworkCore.SqlChangeTracking.Extensions.Internal
{
    public static class DbContextExtensions
    {
        public static Task ResetChangeTracking(this DbContext context, IEntityType entityType)
        {
            var tableName = entityType.GetFullTableName();

            return context.Database.ExecuteSqlRawAsync(@$"ALTER TABLE {tableName}  
                                                          DISABLE CHANGE_TRACKING  

                                                          ALTER TABLE {tableName}  
                                                          ENABLE CHANGE_TRACKING");
        }

        public static async IAsyncEnumerable<IChangeTrackingEntry<T>> ToChangeSet<T>(this DbContext dbContext, string rawSql, bool hasChangeTrackingInfo = true) where T : class, new()
        {
            var entityType = dbContext.Model.FindEntityType(typeof(T));

            var reader = (await dbContext.Database.ExecuteSqlQueryAsync(rawSql).ConfigureAwait(false)).DbDataReader;

            while (await reader.ReadAsync().ConfigureAwait(false))
                yield return mapToChangeTrackingEntry<T>(reader, entityType, hasChangeTrackingInfo);
        }

        static Func<object, object> DefaultValueConverter = o => o == DBNull.Value ? null : o;

        static IChangeTrackingEntry<T> mapToChangeTrackingEntry<T>(DbDataReader reader, IEntityType entityType, bool hasChangeTrackingInfo) where T : class, new()
        {
            var byteArray = hasChangeTrackingInfo ? reader[nameof(ChangeTrackingEntry<T>.ChangeContext)] as byte[] : null;

            var changeContext = hasChangeTrackingInfo ? byteArray == null ? null : Encoding.UTF8.GetString(byteArray) : null;
            var changeVersion = hasChangeTrackingInfo ? reader[nameof(ChangeTrackingEntry<T>.ChangeVersion)] as long? : null;
            var creationVersion = hasChangeTrackingInfo ? reader[nameof(ChangeTrackingEntry<T>.CreationVersion)] as long? : null;

            var operation = hasChangeTrackingInfo ? reader[nameof(ChangeTrackingEntry<T>.ChangeOperation)] as string : null;

            ChangeOperation changeOperation = operation switch
                {
                "I" => ChangeOperation.Insert,
                "U" => ChangeOperation.Update,
                "D" => ChangeOperation.Delete,
                _ => ChangeOperation.None
                };

            var entry = new ChangeTrackingEntry<T>(new T(), changeVersion, creationVersion, changeOperation, changeContext, entityType.GetFullTableName());
            
            var propertyLookup = entityType.GetProperties().ToDictionary(p => p.Name, p => p);

            var tableIdentifier = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table);

            foreach (var propertyInfo in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!propertyLookup.TryGetValue(propertyInfo.Name, out var entityProperty))
                    continue;

                var valueConverter = propertyLookup[propertyInfo.Name].GetValueConverter()?.ConvertFromProvider ?? DefaultValueConverter;
                
                var columnName = entityProperty.GetColumnName(tableIdentifier.Value);

                var value = reader[columnName];

                value = value is DBNull ? null : value;

                object? readerValue = null;

                if (value != null)
                    readerValue = valueConverter(value);

                try
                {
                    propertyInfo.SetValue(entry.Entry, readerValue);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error attempting to set value for property: {propertyInfo.Name} on type: {typeof(T).FullName} value: {readerValue?.ToString()}", ex);
                }
            }

            return entry;
        }

        internal static Task<RelationalDataReader> ExecuteSqlQueryAsync(this DatabaseFacade databaseFacade, string sql, params object[] parameters)
        {
            var concurrencyDetector = databaseFacade.GetService<IConcurrencyDetector>();

            using var cd = concurrencyDetector.EnterCriticalSection();

            var rawSqlCommand = databaseFacade
                .GetService<IRawSqlCommandBuilder>()
                .Build(sql, parameters);

            var paramObject = new RelationalCommandParameterObject(databaseFacade.GetService<IRelationalConnection>(), rawSqlCommand.ParameterValues, null, null, null);

            return rawSqlCommand
                .RelationalCommand
                .ExecuteReaderAsync(paramObject);
        }
    }
}

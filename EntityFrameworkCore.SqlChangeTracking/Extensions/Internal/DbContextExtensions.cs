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
        public static async IAsyncEnumerable<IChangeTrackingEntry<T>> ToChangeSet<T>(this DbContext dbContext, string rawSql) where T : class, new()
        {
            var entityType = dbContext.Model.FindEntityType(typeof(T));

            var reader = (await dbContext.Database.ExecuteSqlQueryAsync(rawSql).ConfigureAwait(false)).DbDataReader;

            while (await reader.ReadAsync().ConfigureAwait(false))
                yield return mapToChangeTrackingEntry<T>(reader, entityType);
        }

        static Func<object, object> DefaultValueConverter = o => o == DBNull.Value ? null : o;

        static IChangeTrackingEntry<T> mapToChangeTrackingEntry<T>(DbDataReader reader, IEntityType entityType) where T : class, new()
        {
            var byteArray = reader[nameof(ChangeTrackingEntry<T>.ChangeContext)] as byte[];

            var changeContext = byteArray == null ? null : Encoding.UTF8.GetString(byteArray);
            var changeVersion = reader[nameof(ChangeTrackingEntry<T>.ChangeVersion)] as long?;
            var creationVersion = reader[nameof(ChangeTrackingEntry<T>.CreationVersion)] as long?;

            var operation = reader[nameof(ChangeTrackingEntry<T>.ChangeOperation)] as string;

            ChangeOperation changeOperation = operation switch
                {
                "I" => ChangeOperation.Insert,
                "U" => ChangeOperation.Update,
                "D" => ChangeOperation.Delete,
                _ => ChangeOperation.None
                };

            var entry = new ChangeTrackingEntry<T>(new T(), changeVersion, creationVersion, changeOperation, changeContext);
            
            var propertyLookup = entityType.GetProperties().ToDictionary(p => p.Name, p => p);

            foreach (var propertyInfo in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!propertyLookup.TryGetValue(propertyInfo.Name, out var entityProperty))
                    continue;

                var valueConverter = propertyLookup[propertyInfo.Name].GetValueConverter()?.ConvertFromProvider ?? DefaultValueConverter;
                
                var columnName = entityProperty.GetColumnName();

                var value = reader[columnName];

                value = value is DBNull ? null : value;

                object? readerValue = null;

                if (value != null)
                    readerValue = valueConverter(value);

                propertyInfo.SetValue(entry.Entity, readerValue);
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

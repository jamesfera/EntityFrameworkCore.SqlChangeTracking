using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkCore.SqlChangeTracking.Extensions
{
    public static class DatabaseFacadeExtensions
    {
        public static async Task<bool> IsChangeTrackingEnabledFor(this DatabaseFacade database, IEntityType entityType)
        {
            var tableName = entityType.GetTableName();

            var sql = $"select ISNULL((SELECT cast('TRUE' as varchar(5))  FROM sys.change_tracking_tables where object_id = OBJECT_ID('{tableName}')),'FALSE') as IsEnabled";

            var result = await database.ExecuteSqlRawAsync(sql);

            return result.ToString() == "TRUE";
        }
    }
}

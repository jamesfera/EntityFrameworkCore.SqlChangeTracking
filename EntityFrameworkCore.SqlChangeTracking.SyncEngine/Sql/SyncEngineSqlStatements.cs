using System;
using EntityFrameworkCore.SqlChangeTracking.Sql;
using EntityFrameworkCore.SqlChangeTracking.SyncEngine.Models;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine.Sql
{
    public static class SyncEngineSqlStatements
    {
        //public static string GetNextChangeVersionExpression(IEntityType entityType, string syncContext)
        //{
        //    var lastChangeVersionExpression = GetLastChangeVersionExpression(entityType, syncContext);

        //    var sql = ChangeTableSqlStatements.GetNextChangeVersionExpression(entityType, lastChangeVersionExpression);

        //    return sql;
        //}

        static int BatchSize = 200;

        public static string GetNextBatchExpression(IEntityType entityType, object? previousPageToken)
        {
            var prefix = "E";

            var columnNames = ChangeTableSqlStatements.GetEntityColumnNames(entityType, prefix, prefix);

            var primaryKeyColumns = entityType.GetPrimaryKeyString(prefix);

            previousPageToken ??= 0;

            var sql = $"SELECT TOP {BatchSize} {columnNames} FROM {entityType.GetFullTableName()} AS {prefix} WHERE {primaryKeyColumns} > {previousPageToken} ORDER BY {primaryKeyColumns}";

            return sql;
        }

        public static string GetNextChangeSetExpression(IEntityType entityType, long? lastChangedVersion)
        {
            var sql = ChangeTableSqlStatements.GetNextChangeSetExpression(entityType, lastChangedVersion);

            return sql;
        }

        public static string GetAllChangeSetsExpression(IEntityType entityType, long? lastChangedVersion)
        {
            var sql = $@"{ChangeTableSqlStatements.GetAllChangeSetsExpression(entityType, lastChangedVersion, false)}
                        WHERE SYS_CHANGE_VERSION > ({lastChangedVersion})
                        ORDER BY SYS_CHANGE_VERSION";

            return sql;
        }

        public static string GetLastChangeVersionExpression(IEntityType entityType, string syncContext)
        {
            var fullTableName = entityType.GetFullTableName();

            return $@"SELECT LastSyncedVersion FROM [dbo].{nameof(LastSyncedChangeVersion)}
			                WHERE TableName = '{fullTableName}' AND SyncContext = '{syncContext}'";
        }
    }
}

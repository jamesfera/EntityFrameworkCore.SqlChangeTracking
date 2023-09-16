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

        public static string GetNextBatchExpression(IEntityType entityType, object? previousPageToken, int batchSize)
        {
            var prefix = "E";

            var primaryKeyColumns = entityType.GetPrimaryKeyString(prefix);

            previousPageToken ??= 0;

            var discriminatorValue = entityType.GetDiscriminatorValue();
            string discriminatorClause = string.Empty;

            if (discriminatorValue != null)
                discriminatorClause = $"AND {entityType.GetDiscriminatorPropertyName()} = '{discriminatorValue}'";

            var sql = $"SELECT TOP {batchSize} {primaryKeyColumns} FROM {entityType.GetFullTableName()} AS {prefix} WHERE {primaryKeyColumns} > {previousPageToken} {discriminatorClause} ORDER BY {primaryKeyColumns}";

            return sql;
        }

        //public static string GetNextChangeSetExpression(IEntityType entityType, long? lastChangedVersion) => ChangeTableSqlStatements.GetNextChangeSetExpression(entityType, lastChangedVersion);

        //public static string GetAllChangeSetsExpression(IEntityType entityType, long? lastChangedVersion)
        //{
        //    var sql = $@"{ChangeTableSqlStatements.GetAllChangeSetsExpression(entityType, lastChangedVersion, false)}
        //                WHERE SYS_CHANGE_VERSION > ({lastChangedVersion})
        //                ORDER BY SYS_CHANGE_VERSION";

        //    return sql;
        //}

        public static string GetLastChangeVersionExpression(IEntityType entityType, string syncContext)
        {
            var fullTableName = entityType.GetFullTableName();

            return $@"SELECT LastSyncedVersion FROM [dbo].{nameof(LastSyncedChangeVersion)}
			                WHERE TableName = '{fullTableName}' AND SyncContext = '{syncContext}'";
        }
    }
}

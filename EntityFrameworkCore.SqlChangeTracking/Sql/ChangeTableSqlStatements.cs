using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkCore.SqlChangeTracking.Sql
{
    public static class ChangeTableSqlStatements
    {
        public const string EntityTablePrefix = "T";
        public const string ChangeTablePrefix = "CT";

        public static string GetNextChangeVersionExpression(IEntityType entityType, long? lastChangedVersion)
        {
            var fullTableName = entityType.GetFullTableName();

            var discriminatorPropertyName = entityType.GetDiscriminatorPropertyName();
            var discriminatorValue = entityType.GetDiscriminatorValue();

            var sql = $@"SELECT MIN(SYS_CHANGE_VERSION) FROM CHANGETABLE(CHANGES {fullTableName}, {lastChangedVersion}) AS {ChangeTablePrefix}";

            if (discriminatorValue != null)
                sql += $" LEFT OUTER JOIN {fullTableName} AS {EntityTablePrefix}1 ON ({EntityTablePrefix}1.[Id] = {ChangeTablePrefix}.[Id])";

            sql += $" WHERE SYS_CHANGE_VERSION > ({lastChangedVersion})";

            if (discriminatorValue != null)
                sql += $" AND {EntityTablePrefix}1.{discriminatorPropertyName} = '{discriminatorValue}'";

            return sql;
        }

        public static string GetNextChangeSetExpression(IEntityType entityType, long? lastChangedVersion)
        {
            var allChangesSql = GetAllChangeSetsExpression(entityType, lastChangedVersion, false);

            var sql = $@"{allChangesSql}
                            {(allChangesSql.Contains("WHERE") ? "AND" : "WHERE")} SYS_CHANGE_VERSION = ({GetNextChangeVersionExpression(entityType, lastChangedVersion)})";

            return sql;
        }

        public static string GetAllChangeSetsExpression(IEntityType entityType, long? lastChangedVersion, bool terminate)
        {
            var fullTableName = entityType.GetFullTableName();

            var entityColumnNames = GetEntityColumnNames(entityType, ChangeTablePrefix, EntityTablePrefix);

            var columnNames = entityColumnNames.Append(", ").Append(ChangeTrackingColumnNames);

            var onExpression = GetJoinConditionExpression(entityType, EntityTablePrefix, ChangeTablePrefix);

            var sql = $@"SELECT {columnNames}
                         FROM CHANGETABLE(CHANGES {fullTableName}, {lastChangedVersion}) AS {ChangeTablePrefix}
                         LEFT OUTER JOIN
                         {fullTableName} AS {EntityTablePrefix} ON ({onExpression})
                         ";

            var discriminatorPropertyName = entityType.GetDiscriminatorPropertyName();

            if (discriminatorPropertyName != null)
            {
                var discriminatorValue = entityType.GetDiscriminatorValue();
                sql += $" WHERE {discriminatorPropertyName} = '{discriminatorValue}'";
            }

            if (terminate)
                sql += " ORDER BY SYS_CHANGE_VERSION";

            return sql;
        }

        public static StringBuilder GetEntityColumnNames(IEntityType entityType, string pkPrefix, string columnPrefix)
        {
            StringBuilder sb = new StringBuilder();

            //primary key columns
            sb.Append(string.Join(",", entityType.GetPrimaryKeyColumnNames().Select(p => $"{pkPrefix}.[{p}]")));

            var baseType = entityType.BaseType;

            while (baseType != null)
            {
                var basePrefix = "B1";

                var baseColumns = baseType.GetDeclaredColumnNames(true);

                foreach (var baseColumn in baseColumns)
                {
                    sb.Append(",");
                    sb.Append($"(SELECT {basePrefix}.[{baseColumn}] FROM [{baseType.GetTableName()}] AS {basePrefix} WHERE {GetJoinConditionExpression(baseType, basePrefix, columnPrefix)}) AS {baseColumn}");
                }

                baseType = baseType.BaseType;
            }

            var entityColumns = entityType.GetDeclaredColumnNames(true).Select(c => $"{columnPrefix}.[{c}]");

            if (entityColumns.Any())
            {
                sb.Append(",");
                sb.Append(string.Join(",", entityColumns));
            }

            return sb;
        }

        static readonly StringBuilder ChangeTrackingColumnNames = new StringBuilder("SYS_CHANGE_VERSION as ChangeVersion, SYS_CHANGE_CREATION_VERSION as CreationVersion, SYS_CHANGE_OPERATION as ChangeOperation, SYS_CHANGE_CONTEXT as ChangeContext");

        public static string GetJoinConditionExpression(IEntityType entityType, params string[] prefixes)
        {
            return string.Join(" AND ", entityType.GetPrimaryKeyColumnNames().Select(name => string.Join(" = ", prefixes.Select(p => $"{p}.[{name}]"))));
        }
    }
}

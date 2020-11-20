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

        public static string GetNextChangeVersionExpression(IEntityType entityType, string lastChangeVersionExpression)
        {
            var fullTableName = entityType.GetFullTableName();

            var sql = $@"SELECT MIN(SYS_CHANGE_VERSION) FROM CHANGETABLE(CHANGES {fullTableName}, 0) AS {ChangeTablePrefix}
                        WHERE SYS_CHANGE_VERSION > ({lastChangeVersionExpression})";

            return sql;
        }

        public static string GetNextChangeSetExpression(IEntityType entityType, string lastChangeVersionExpression)
        {
            var sql = $@"{GetAllChangeSetsExpression(entityType, false)}
                         WHERE SYS_CHANGE_VERSION = ({GetNextChangeVersionExpression(entityType, lastChangeVersionExpression)})";

            return sql;
        }

        public static string GetAllChangeSetsExpression(IEntityType entityType, bool terminate, long version = 0)
        {
            var fullTableName = entityType.GetFullTableName();

            var columnNames = GetColumnNamesString(entityType, EntityTablePrefix, ChangeTablePrefix);

            var onExpression = GetJoinConditionExpression(entityType, EntityTablePrefix, ChangeTablePrefix);

            var sql = $@"SELECT {columnNames}
                         FROM CHANGETABLE(CHANGES {fullTableName}, {version}) AS {ChangeTablePrefix}
                         LEFT OUTER JOIN
                         {fullTableName} AS {EntityTablePrefix} ON ({onExpression})";

            if (terminate)
                sql += " ORDER BY SYS_CHANGE_VERSION";

            return sql;
        }

        public static string GetColumnNamesString(IEntityType entityType, string entityTablePrefix, string changeTablePrefix)
        {
            StringBuilder sb = new StringBuilder();

            
            //primary key columns
            sb.Append(string.Join(",", entityType.GetPrimaryKeyColumnNames(changeTablePrefix)));

            var baseType = entityType.BaseType;

            while (baseType != null)
            {
                var basePrefix = "B1";

                var baseColumns = baseType.GetDeclaredColumnNames(true);

                foreach (var baseColumn in baseColumns)
                {
                    sb.Append(",");
                    sb.Append($"(SELECT {basePrefix}.{baseColumn} FROM {baseType.GetTableName()} AS {basePrefix} WHERE {GetJoinConditionExpression(baseType, basePrefix, entityTablePrefix)}) AS {baseColumn}");
                }

                baseType = baseType.BaseType;
            }

            var entityColumns = entityType.GetDeclaredColumnNames(true).Select(c => $"{entityTablePrefix}.{c}");

            if (entityColumns.Any())
            {
                sb.Append(",");
                sb.Append(string.Join(",", entityColumns));
            }

            sb.Append(",");

            //change table columns
            sb.Append("SYS_CHANGE_VERSION as ChangeVersion, SYS_CHANGE_CREATION_VERSION as CreationVersion, SYS_CHANGE_OPERATION as ChangeOperation, SYS_CHANGE_CONTEXT as ChangeContext");


            return sb.ToString();
        }

        public static string GetJoinConditionExpression(IEntityType entityType, params string[] prefixes)
        {
            return string.Join(" AND ", entityType.GetPrimaryKeyColumnNames().Select(name => string.Join(" = ", prefixes.Select(p => $"{p}.{name}"))));
        }
    }
}

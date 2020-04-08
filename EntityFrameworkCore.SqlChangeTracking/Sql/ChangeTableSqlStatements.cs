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

        public static string GetNextChangeVersion(IEntityType entityType, string lastChangeVersionExpression)
        {
            var fullTableName = entityType.GetFullTableName();

            var sql = $@"SELECT MIN(SYS_CHANGE_VERSION) as NextVersion FROM {fullTableName} as {EntityTablePrefix}
                        CROSS APPLY CHANGETABLE(VERSION {fullTableName}, ({entityType.GetPrimaryKeyString()}), ({entityType.GetPrimaryKeyString(EntityTablePrefix)})) AS {ChangeTablePrefix}
                        WHERE SYS_CHANGE_VERSION > ({lastChangeVersionExpression})";

            return sql;
        }

        public static string GetNextChangeSet(IEntityType entityType, string lastChangedVersionExpression, string nextChangeVersionExpression)
        {
            var fullTableName = entityType.GetFullTableName();

            var columnNames = GetColumnNamesString(entityType, EntityTablePrefix, ChangeTablePrefix);

            var onExpression = GetJoinConditionExpression(entityType, EntityTablePrefix, ChangeTablePrefix);

            var sql = $@"SELECT {columnNames} FROM CHANGETABLE(CHANGES {fullTableName}, {lastChangedVersionExpression}) AS {ChangeTablePrefix}
                         LEFT OUTER JOIN
                         {fullTableName} AS {EntityTablePrefix} ON ({onExpression})
                         WHERE SYS_CHANGE_VERSION = ({nextChangeVersionExpression})";

            return sql;
        }

        public static string GetColumnNamesString(IEntityType entityType, string entityTablePrefix, string changeTablePrefix)
        {
            StringBuilder sb = new StringBuilder();

            //primary key columns
            sb.Append(string.Join(",", entityType.FindPrimaryKey().Properties.Select(c => $"{changeTablePrefix}.{c.GetColumnName()}")));

            sb.Append(",");

            //entity columns
            sb.Append(string.Join(",", entityType.GetColumnNames(true).Select(c => $"{entityTablePrefix}.{c}")));

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

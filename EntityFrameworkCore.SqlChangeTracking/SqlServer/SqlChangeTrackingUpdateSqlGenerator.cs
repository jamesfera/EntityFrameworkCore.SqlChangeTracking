using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Update.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.SqlChangeTracking.SqlServer
{
    public class SqlChangeTrackingUpdateSqlGenerator : SqlServerUpdateSqlGenerator
    {
        public SqlChangeTrackingUpdateSqlGenerator(UpdateSqlGeneratorDependencies dependencies) : base(dependencies) { }

        public override ResultSetMapping AppendUpdateOperation(StringBuilder commandStringBuilder, ModificationCommand command, int commandPosition)
        {
            var tableName = command.TableName;

            var tableTrackingContext = TrackingContextAsyncLocalCache.GetTrackingContextForTable(tableName);

            if (tableTrackingContext != null)
                commandStringBuilder.AppendLine($"DECLARE @dc varbinary(128) = CONVERT(VARBINARY(128), '{tableTrackingContext}');\nWITH CHANGE_TRACKING_CONTEXT( @dc )");

            return base.AppendUpdateOperation(commandStringBuilder, command, commandPosition);
        }

        public override ResultSetMapping AppendBulkInsertOperation(StringBuilder commandStringBuilder, IReadOnlyList<ModificationCommand> modificationCommands, int commandPosition)
        {
            var tableName = modificationCommands.First().TableName;

            var tableTrackingContext = TrackingContextAsyncLocalCache.GetTrackingContextForTable(tableName);

            if (tableTrackingContext != null)
                commandStringBuilder.AppendLine($"DECLARE @dc varbinary(128) = CONVERT(VARBINARY(128), '{tableTrackingContext}');\nWITH CHANGE_TRACKING_CONTEXT( @dc )");

            var result = base.AppendBulkInsertOperation(commandStringBuilder, modificationCommands, commandPosition);

            return result;
        }

        //private void GenerateIdentityInsert(
        //    StringBuilder builder,
        //    string table,
        //    string schema,
        //    IEnumerable<string> columns,
        //    bool on)
        //{
        //    var stringTypeMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));

        //    builder.Append("IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE").Append(" [name] IN (")
        //        .Append(string.Join(", ", columns.Select(stringTypeMapping.GenerateSqlLiteral)))
        //        .Append(") AND [object_id] = OBJECT_ID(").Append(
        //            stringTypeMapping.GenerateSqlLiteral(
        //                Dependencies.SqlGenerationHelper.DelimitIdentifier(table, schema))).AppendLine("))");

        //    builder.Append("SET IDENTITY_INSERT ")
        //        .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(table, schema)).Append(on ? " ON" : " OFF")
        //        .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
        //}
    }
}

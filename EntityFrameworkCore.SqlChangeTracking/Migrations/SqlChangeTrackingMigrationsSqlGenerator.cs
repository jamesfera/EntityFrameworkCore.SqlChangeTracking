using System;
using System.Reflection;
using EntityFrameworkCore.SqlChangeTracking.Migrations.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

namespace EntityFrameworkCore.SqlChangeTracking.Migrations
{
    public class SqlChangeTrackingMigrationsSqlGenerator : SqlServerMigrationsSqlGenerator
    {
        public SqlChangeTrackingMigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies, ICommandBatchPreparer commandBatchPreparer) : base(dependencies, commandBatchPreparer) { }
        
        void Generate(EnableChangeTrackingForDatabaseOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            var sqlHelper = Dependencies.SqlGenerationHelper;

            var autoCleanUp = operation.AutoCleanup ? "ON" : "OFF";

            builder
                .Append("ALTER DATABASE ")
                .Append(sqlHelper.DelimitIdentifier(Dependencies.CurrentContext.Context.Database.GetDbConnection().Database))
                .Append(" SET CHANGE_TRACKING = ON ")
                .Append($"(CHANGE_RETENTION = {operation.RetentionDays} DAYS, AUTO_CLEANUP = {autoCleanUp})")
                .AppendLine(sqlHelper.StatementTerminator)
                .EndCommand(true);
        }

        void Generate(SnapshotIsolationOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            var sqlHelper = Dependencies.SqlGenerationHelper;

            builder
                .Append("ALTER DATABASE ")
                .Append(sqlHelper.DelimitIdentifier(Dependencies.CurrentContext.Context.Database.GetDbConnection().Database))
                .Append($" SET ALLOW_SNAPSHOT_ISOLATION {(operation.Enabled ? "ON" : "OFF")}")
                .AppendLine(sqlHelper.StatementTerminator)
                .EndCommand(true);
        }

        void Generate(DisableChangeTrackingForDatabaseOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            var sqlHelper = Dependencies.SqlGenerationHelper;

            builder
                .Append("ALTER DATABASE ")
                .Append(sqlHelper.DelimitIdentifier(Dependencies.CurrentContext.Context.Database.GetDbConnection().Database))
                .Append(" SET CHANGE_TRACKING = OFF")
                .AppendLine(sqlHelper.StatementTerminator)
                .EndCommand(true);
        }

        void Generate(EnableChangeTrackingForTableOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            //TODO detect if operation.TrackColumns has changed and re-create?

            var tableName = operation.Schema == null ? operation.Table : $"{operation.Schema}.{operation.Table}";

            var sqlHelper = Dependencies.SqlGenerationHelper;

            if (operation.Idempotent)
                AppendChangeTrackingExistsCheck(builder, tableName, exists: false);

            builder
                .Append("ALTER TABLE ")
                .Append(sqlHelper.DelimitIdentifier(tableName))
                .Append(" ENABLE CHANGE_TRACKING");

            if (operation.TrackColumns)
                builder.Append(" WITH (TRACK_COLUMNS_UPDATED = ON)");

            builder.AppendLine(sqlHelper.StatementTerminator)
                .EndCommand();
        }

        void Generate(DisableChangeTrackingForTableOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            var tableName = operation.Schema == null ? operation.Table : $"{operation.Schema}.{operation.Table}";

            var sqlHelper = Dependencies.SqlGenerationHelper;

            if (operation.Idempotent)
                AppendChangeTrackingExistsCheck(builder, tableName, exists: true);

            builder
                .Append("ALTER TABLE ")
                .Append(sqlHelper.DelimitIdentifier(tableName))
                .Append(" DISABLE CHANGE_TRACKING")
                .AppendLine(sqlHelper.StatementTerminator)
                .EndCommand();
        }

        /// <summary>
        /// Appends an IF [NOT] EXISTS check against sys.change_tracking_tables so the following statement
        /// only runs when the table is (or is not) already change tracked.
        /// </summary>
        void AppendChangeTrackingExistsCheck(MigrationCommandListBuilder builder, string tableName, bool exists)
        {
            var sqlHelper = Dependencies.SqlGenerationHelper;
            var stringMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));

            builder
                .Append(exists ? "IF EXISTS" : "IF NOT EXISTS")
                .Append(" (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID(")
                .Append(stringMapping.GenerateSqlLiteral(sqlHelper.DelimitIdentifier(tableName)))
                .AppendLine("))")
                .Append("    ");
        }

        protected override void Generate(MigrationOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            var typesToScan = new List<Type>();

            var type = GetType();

            while (type != typeof(SqlServerMigrationsSqlGenerator) && type is not null)
            {
                typesToScan.Add(type);
                type = type.BaseType;
            }

            var generateMethod = typesToScan
                .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                .Where(m => m.Name == nameof(Generate))
                .FirstOrDefault(m => m.GetParameters().Select(p => p.ParameterType).Contains(operation.GetType()));

            if (generateMethod is not null)
                generateMethod.Invoke(this, new object?[] { operation, model, builder });
            else
                base.Generate(operation, model, builder);
        }
    }
}

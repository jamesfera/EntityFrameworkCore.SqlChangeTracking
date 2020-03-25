using System;
using System.Collections.Generic;
using System.Text;
using EntityFrameworkCore.SqlChangeTracking.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EntityFrameworkCore.SqlChangeTracking.Migrations
{
    public class SqlChangeTrackingMigrationsSqlGenerator : SqlServerMigrationsSqlGenerator
    {
        public SqlChangeTrackingMigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies, IMigrationsAnnotationProvider migrationsAnnotations) : base(dependencies, migrationsAnnotations) { }

        //protected override void Generate(AlterDatabaseOperation operation, IModel model, MigrationCommandListBuilder builder)
        //{
        //    var serviceBrokerEnabled = operation.IsServiceBrokerEnabled();
        //    var serviceBrokerWasEnabled = operation.OldDatabase.FindAnnotation(SyncEngineAnnotationNames.ServiceBroker)?.Value as bool? ?? false;

        //    if (!serviceBrokerEnabled && !serviceBrokerWasEnabled)
        //    {
        //        base.Generate(operation, model, builder);
        //        return;
        //    }

        //    var sqlHelper = Dependencies.SqlGenerationHelper;
        //    var database = Dependencies.CurrentContext.Context.Database.GetDbConnection().Database;

        //    if (serviceBrokerEnabled)
        //    {
        //        builder
        //            .Append("ALTER DATABASE ")
        //            .Append(sqlHelper.DelimitIdentifier(database))
        //            .Append(" SET ALLOW_SNAPSHOT_ISOLATION ON ")
        //            .AppendLine(sqlHelper.StatementTerminator)
        //            .EndCommand();
        //    }
        //    else
        //    {
        //        builder
        //            .Append("ALTER DATABASE ")
        //            .Append(sqlHelper.DelimitIdentifier(database))
        //            .Append(" SET ALLOW_SNAPSHOT_ISOLATION OFF ")
        //            .AppendLine(sqlHelper.StatementTerminator)
        //            .EndCommand();
        //    }
        //}

        protected override void Generate(AlterDatabaseOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            var changeTrackingEnabled = operation.IsChangeTrackingEnabled();
            var changeTrackingWasEnabled = operation.OldDatabase.FindAnnotation(SqlChangeTrackingAnnotationNames.Enabled)?.Value as bool? ?? false;

            if (!changeTrackingEnabled && !changeTrackingWasEnabled)
            {
                base.Generate(operation, model, builder);
                return;
            }

            var sqlHelper = Dependencies.SqlGenerationHelper;

            if (changeTrackingEnabled)
            {
                var autoCleanUp = operation.ChangeTrackingAutoCleanUp() ? "ON" : "OFF";

                builder
                    .Append("ALTER DATABASE ")
                    .Append(sqlHelper.DelimitIdentifier(Dependencies.CurrentContext.Context.Database.GetDbConnection().Database))
                    .Append(" SET CHANGE_TRACKING = ON ")
                    .Append($"(CHANGE_RETENTION = {operation.ChangeTrackingRetentionDays()} DAYS, AUTO_CLEANUP = {autoCleanUp})")
                    .AppendLine(sqlHelper.StatementTerminator)
                    .EndCommand(true);
            }
            else
            {
                builder
                    .Append("ALTER DATABASE ")
                    .Append(sqlHelper.DelimitIdentifier(Dependencies.CurrentContext.Context.Database.GetDbConnection().Database))
                    .Append(" SET CHANGE_TRACKING = OFF ")
                    .AppendLine(sqlHelper.StatementTerminator)
                    .EndCommand(true);
            }
        }

        protected override void Generate(AlterTableOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            var changeTrackingEnabled = operation.IsChangeTrackingEnabled();
            var changeTrackingWasEnabled = operation.OldTable.IsChangeTrackingEnabled();

            if (!changeTrackingEnabled && !changeTrackingWasEnabled)
            {
                base.Generate(operation, model, builder);
                return;
            }

            var tableName = operation.Schema == null ? operation.Name : $"{operation.Schema}.{operation.Name}";

            var sqlHelper = Dependencies.SqlGenerationHelper;

            void DisableChangeTracking()
            {
                builder
                    .Append("ALTER TABLE ")
                    .Append(sqlHelper.DelimitIdentifier(tableName))
                    .Append(" DISABLE CHANGE_TRACKING ")
                    .AppendLine(sqlHelper.StatementTerminator)
                    .EndCommand();
            }

            if (changeTrackingEnabled)
            {
                var trackColumns = operation.ChangeTrackingTrackColumns();
                var oldTrackColumns = changeTrackingWasEnabled && operation.OldTable.ChangeTrackingTrackColumns();

                if (changeTrackingWasEnabled && trackColumns != oldTrackColumns)
                    DisableChangeTracking();

                builder
                    .Append("ALTER TABLE ")
                    .Append(sqlHelper.DelimitIdentifier(tableName))
                    .Append(" ENABLE CHANGE_TRACKING ");

                if (trackColumns)
                    builder.Append("WITH (TRACK_COLUMNS_UPDATED = ON)");

                builder.AppendLine(sqlHelper.StatementTerminator)
                    .EndCommand();
            }
            else
                DisableChangeTracking();
        }
    }
}

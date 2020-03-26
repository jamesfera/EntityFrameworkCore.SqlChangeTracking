using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using EntityFrameworkCore.SqlChangeTracking.Extensions;
using EntityFrameworkCore.SqlChangeTracking.Migrations.Operations;
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

            var tableName = operation.Schema == null ? operation.Name : $"{operation.Schema}.{operation.Name}";

            var sqlHelper = Dependencies.SqlGenerationHelper;

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
            var tableName = operation.Schema == null ? operation.Name : $"{operation.Schema}.{operation.Name}";

            var sqlHelper = Dependencies.SqlGenerationHelper;

            builder
                .Append("ALTER TABLE ")
                .Append(sqlHelper.DelimitIdentifier(tableName))
                .Append(" DISABLE CHANGE_TRACKING")
                .AppendLine(sqlHelper.StatementTerminator)
                .EndCommand();
        }

        protected override void Generate(MigrationOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            Action generateAction = operation switch
            {
                EnableChangeTrackingForDatabaseOperation op => () => Generate(op, model, builder),
                DisableChangeTrackingForDatabaseOperation op => () => Generate(op, model, builder),
                EnableChangeTrackingForTableOperation op => () => Generate(op, model, builder),
                DisableChangeTrackingForTableOperation op => () => Generate(op, model, builder),
                _ => () => base.Generate(operation, model, builder)
            };

            generateAction();
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
        }
    }
}

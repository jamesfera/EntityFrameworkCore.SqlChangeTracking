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
        public SqlChangeTrackingMigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies, IRelationalAnnotationProvider migrationsAnnotations) : base(dependencies, migrationsAnnotations) { }
        
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

        void Generate(EnableSnapshotIsolationOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            var sqlHelper = Dependencies.SqlGenerationHelper;

            builder
                .Append("ALTER DATABASE ")
                .Append(sqlHelper.DelimitIdentifier(Dependencies.CurrentContext.Context.Database.GetDbConnection().Database))
                .Append(" SET ALLOW_SNAPSHOT_ISOLATION ON")
                .AppendLine(sqlHelper.StatementTerminator)
                .EndCommand(true);

            //    builder
            //        .Append("ALTER DATABASE ")
            //        .Append(sqlHelper.DelimitIdentifier(database))
            //        .Append(" SET ALLOW_SNAPSHOT_ISOLATION OFF ")
            //        .AppendLine(sqlHelper.StatementTerminator)
            //        .EndCommand();
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

        protected override void Generate(AlterDatabaseOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            base.Generate(operation, model, builder);

            if (operation.IsChangeTrackingEnabled())
                Generate(new EnableChangeTrackingForDatabaseOperation(operation.ChangeTrackingRetentionDays(), operation.ChangeTrackingAutoCleanUp()), model, builder);
            else if(operation.OldDatabase?.FindAnnotation(SqlChangeTrackingAnnotationNames.Enabled)?.Value as bool? ?? false)
                Generate(new DisableChangeTrackingForDatabaseOperation(), model, builder);

            if (operation.IsSnapshotIsolationEnabled())
                Generate(new EnableSnapshotIsolationOperation(), model, builder);
            //else if (operation.OldDatabase?.FindAnnotation(SqlChangeTrackingAnnotationNames.SnapshotIsolation)?.Value as bool? ?? false)
            //    Generate(new DisableSnapshotIsolationOperation(), model, builder);
        }

        protected override void Generate(CreateTableOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate = true)
        {
           base.Generate(operation, model, builder);

           if (operation.IsChangeTrackingEnabled())
               Generate(new EnableChangeTrackingForTableOperation(operation.Name, operation.Schema, operation.ChangeTrackingTrackColumns()), model, builder);
        }

        protected override void Generate(AlterTableOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            base.Generate(operation, model, builder);

            if (operation.IsChangeTrackingEnabled())
                Generate(new EnableChangeTrackingForTableOperation(operation.Name, operation.Schema, operation.ChangeTrackingTrackColumns()), model, builder);
            else if(operation.OldTable.IsChangeTrackingEnabled())
                Generate(new DisableChangeTrackingForTableOperation(operation.Name, operation.Schema), model, builder);
        }

        protected override void Generate(MigrationOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            Action generateAction = operation switch
            {
                EnableChangeTrackingForDatabaseOperation op => () => Generate(op, model, builder),
                DisableChangeTrackingForDatabaseOperation op => () => Generate(op, model, builder),
                EnableChangeTrackingForTableOperation op => () => Generate(op, model, builder),
                DisableChangeTrackingForTableOperation op => () => Generate(op, model, builder),
                EnableSnapshotIsolationOperation op => () => Generate(op, model, builder),
                _ => () => base.Generate(operation, model, builder)
            };

            generateAction();
        }
    }
}

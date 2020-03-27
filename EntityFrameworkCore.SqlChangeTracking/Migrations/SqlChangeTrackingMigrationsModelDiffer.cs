using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using EntityFrameworkCore.SqlChangeTracking.Extensions;
using EntityFrameworkCore.SqlChangeTracking.Migrations.Operations;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;

namespace EntityFrameworkCore.SqlChangeTracking.Migrations
{
    public class SqlChangeTrackingMigrationsModelDiffer : MigrationsModelDiffer
    {
        public SqlChangeTrackingMigrationsModelDiffer(IRelationalTypeMappingSource typeMappingSource,
            IMigrationsAnnotationProvider migrationsAnnotations, IChangeDetector changeDetector,
            IUpdateAdapterFactory updateAdapterFactory,
            CommandBatchPreparerDependencies commandBatchPreparerDependencies) : base(typeMappingSource,
            migrationsAnnotations, changeDetector, updateAdapterFactory, commandBatchPreparerDependencies)
        {
        }

        protected override IEnumerable<MigrationOperation> Diff(TableMapping source, TableMapping target, DiffContext diffContext)
        {
            var operations = base.Diff(source, target, diffContext);
            
            foreach (var migrationOperation in operations)
            {
                if (migrationOperation is AlterTableOperation tableOperation)
                {
                    if(tableOperation.IsChangeTrackingEnabled())
                        yield return new EnableChangeTrackingForTableOperation(target.Name, target.Schema, tableOperation.ChangeTrackingTrackColumns());
                    else if (tableOperation.OldTable.IsChangeTrackingEnabled())
                        yield return new DisableChangeTrackingForTableOperation(target.Name, target.Schema);
                    else
                        yield return migrationOperation;
                }
                else
                    yield return migrationOperation;
            }
        }
        
        protected override IEnumerable<MigrationOperation> Diff(IModel source, IModel target, DiffContext diffContext)
        {
            var operations = base.Diff(source, target, diffContext);

            foreach (var migrationOperation in operations)
            {
                if (migrationOperation is AlterDatabaseOperation dbOperation)
                {
                    if(dbOperation.IsChangeTrackingEnabled())
                        yield return new EnableChangeTrackingForDatabaseOperation(dbOperation.ChangeTrackingRetentionDays(), dbOperation.ChangeTrackingAutoCleanUp());
                    else if(dbOperation.OldDatabase.FindAnnotation(SqlChangeTrackingAnnotationNames.Enabled)?.Value as bool? ?? false)
                        yield return new DisableChangeTrackingForDatabaseOperation();
                    else
                        yield return migrationOperation;
                }
                else
                    yield return migrationOperation;
            }
        }

        protected override IEnumerable<MigrationOperation> Add(TableMapping target, DiffContext diffContext)
        {
            var operations = base.Add(target, diffContext);

            foreach (var migrationOperation in operations)
            {
                if (migrationOperation is CreateTableOperation tableOperation)
                {
                    yield return migrationOperation;

                    if (tableOperation.IsChangeTrackingEnabled())
                        yield return new EnableChangeTrackingForTableOperation(target.Name, target.Schema, tableOperation.ChangeTrackingTrackColumns());
                }
                else
                    yield return migrationOperation;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityFrameworkCore.SqlChangeTracking.Extensions;
using EntityFrameworkCore.SqlChangeTracking.Migrations.Operations;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
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
                if (migrationOperation is TableOperation ctOperation && ctOperation.IsChangeTrackingEnabled())
                {
                    var enableOperation = new EnableChangeTrackingForTableOperation(target.Name, target.Schema);

                    yield return enableOperation;
                }
            }

            yield break;
        }
    }
}

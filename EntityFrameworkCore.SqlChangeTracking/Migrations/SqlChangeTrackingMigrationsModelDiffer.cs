using EntityFrameworkCore.SqlChangeTracking.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update.Internal;

namespace EntityFrameworkCore.SqlChangeTracking.Migrations
{
    public class SqlChangeTrackingMigrationsModelDiffer(
            IRelationalTypeMappingSource typeMappingSource,
            IMigrationsAnnotationProvider migrationsAnnotationProvider,
            IRowIdentityMapFactory rowIdentityMapFactory,
            CommandBatchPreparerDependencies commandBatchPreparerDependencies)
        : MigrationsModelDiffer(typeMappingSource, migrationsAnnotationProvider, rowIdentityMapFactory, commandBatchPreparerDependencies)
    {
        protected override IEnumerable<MigrationOperation> Diff(ITable source, ITable target, DiffContext diffContext)
        {
            var operations = base.Diff(source, target, diffContext);

            var targetEntityType = (IEntityType)target.EntityTypeMappings.First().TypeBase;
            var sourceEntityType = (IEntityType)source.EntityTypeMappings.First().TypeBase;

            if (targetEntityType.IsSqlChangeTrackingEnabled() && !sourceEntityType.IsSqlChangeTrackingEnabled())
                operations = operations.Concat(new[] { new EnableChangeTrackingForTableOperation(target.Name, target.Schema, targetEntityType.ChangeTrackingTrackColumns()) });
            else if (!targetEntityType.IsSqlChangeTrackingEnabled() && sourceEntityType.IsSqlChangeTrackingEnabled())
                operations = operations.Concat(new[] { new DisableChangeTrackingForTableOperation(target.Name, target.Schema) });

            return operations;
        }

        protected override IEnumerable<MigrationOperation> Diff(IRelationalModel? source, IRelationalModel? target, DiffContext diffContext)
        {
            var operations = base.Diff(source, target, diffContext);

            if ((target?.Model.IsSqlChangeTrackingEnabled() ?? false) && !(source?.Model.IsSqlChangeTrackingEnabled() ?? false))
                operations = operations.Concat(new[] { new EnableChangeTrackingForDatabaseOperation(target.Model.GetChangeTrackingRetentionDays(), target.Model.IsChangeTrackingAutoCleanupEnabled()) });
            else if (!(target?.Model.IsSqlChangeTrackingEnabled() ?? false) && (source?.Model.IsSqlChangeTrackingEnabled() ?? false))
                operations = operations.Concat(new[] { new DisableChangeTrackingForDatabaseOperation() });

            if ((target?.Model.IsSnapshotIsolationEnabled() ?? false) && !(source?.Model.IsSnapshotIsolationEnabled() ?? false))
                operations = operations.Concat(new[] { new SnapshotIsolationOperation(true) });
            else if (!(target?.Model.IsSnapshotIsolationEnabled() ?? false) && (source?.Model.IsSnapshotIsolationEnabled() ?? false))
                operations = operations.Concat(new[] { new SnapshotIsolationOperation(false) });

            return operations;
        }

        protected override IEnumerable<MigrationOperation> Add(ITable target, DiffContext diffContext)
        {
            var operations = base.Add(target, diffContext);

            var targetEntityType = (IEntityType)target.EntityTypeMappings.First().TypeBase;

            if (targetEntityType.IsSqlChangeTrackingEnabled())
                operations = operations.Concat(new[] { new EnableChangeTrackingForTableOperation(target.Name, target.Schema, targetEntityType.ChangeTrackingTrackColumns()) });

            return operations;
        }
    }
}

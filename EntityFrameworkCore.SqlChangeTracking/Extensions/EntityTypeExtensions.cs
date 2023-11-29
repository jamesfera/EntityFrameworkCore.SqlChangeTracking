using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;


namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class EntityTypeExtensions
    {
        public static bool IsSqlChangeTrackingEnabled(this IEntityType entityType)
            => entityType[SqlChangeTrackingAnnotationNames.Enabled] as bool? ?? false;

        public static void EnableSqlChangeTracking(this IMutableEntityType entityType)
            => entityType.SetOrRemoveAnnotation(SqlChangeTrackingAnnotationNames.Enabled, true);

        public static bool ChangeTrackingTrackColumns(this IEntityType entityType)
            => entityType.FindAnnotation(SqlChangeTrackingAnnotationNames.TrackColumns)?.Value as bool? ?? false;

        public static void SetTrackColumns(this IMutableEntityType entityType, bool enabled = false)
            => entityType.SetOrRemoveAnnotation(SqlChangeTrackingAnnotationNames.TrackColumns, enabled);

        public static string[] GetColumnNames(this IEntityType entityType)
        {
            var tableIdentifier = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table);

            return entityType.GetProperties().Select(p => p.GetColumnName(tableIdentifier.Value)).ToArray();
        }

        public static string GetActualSchema(this IEntityType entityType)
        {
            return string.IsNullOrEmpty(entityType.GetSchema()) ? string.IsNullOrEmpty(entityType.GetDefaultSchema()) ? "dbo" : entityType.GetDefaultSchema() : entityType.GetSchema();
        }

        public static string GetFullTableName(this IEntityType entityType)
        {
            return $"{entityType.GetActualSchema()}.{entityType.GetTableName()}";
        }

        public static string GetPrimaryKeyString(this IEntityType entityType, string prefix = null)
        {
            return string.Join(",", GetPrimaryKeyColumnNames(entityType, prefix));
        }

        public static string[] GetPrimaryKeyColumnNames(this IEntityType entityType, string prefix = null)
        {
            prefix = prefix == null ? "" : $"{prefix}.";

            return  entityType.FindPrimaryKey().Properties.Select(p => $"{prefix}{p.GetColumnName(StoreObjectIdentifier.Create(entityType, StoreObjectType.Table).Value)}").ToArray();
        }

        public static string[] GetColumnNames(this IEntityType entityType, bool excludePrimaryKeyColumns)
        {
            var tableIdentifier = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table);

            var primaryKeyColumnNames = entityType.FindPrimaryKey().Properties.Select(p => p.GetColumnName(tableIdentifier.Value)).ToArray();

            return entityType.GetColumnNames().Where(c => !primaryKeyColumnNames.Contains(c)).ToArray();
        }

        public static string[] GetDeclaredColumnNames(this IEntityType entityType, bool excludePrimaryKeyColumns)
        {
            var tableIdentifier = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table);

            var primaryKeyColumnNames = entityType.FindPrimaryKey().Properties.Select(p => p.GetColumnName(tableIdentifier.Value)).ToArray();

            return entityType.GetDeclaredProperties().Select(p => p.GetColumnName(tableIdentifier.Value)).Where(c => !primaryKeyColumnNames.Contains(c)).ToArray();
        }
    }
}

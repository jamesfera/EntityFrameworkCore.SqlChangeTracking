using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;


namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine
{
    public static class EntityTypeExtensions
    {
        public static bool IsSyncEngineEnabled(this IEntityType entityType)
            => entityType[SyncEngineAnnotationNames.Enabled] as bool? ?? false;

        public static void EnableSyncEngine(this IMutableEntityType entityType, string syncContext)
        {
            entityType.SetAnnotation(SyncEngineAnnotationNames.Enabled, true);

            var syncContextUpper = syncContext.ToUpper();

            if(entityType.GetSyncContexts().Select(c => c.ToUpper()).Contains(syncContextUpper))
                return;

            var nextIndex = 1;

            var syncAnnotationsIndexes = entityType.GetAnnotations().Where(a => a.Name.StartsWith("SyncEngineContext_")).Select(a => int.Parse(a.Name.Replace("SyncEngineContext_", ""))).ToArray();

            if (syncAnnotationsIndexes.Any())
                nextIndex = syncAnnotationsIndexes.Max() + 1;

            var annotationName = $"SyncEngineContext_{nextIndex}";

            entityType.SetAnnotation(annotationName, syncContext);
        }

        public static string[] GetSyncContexts(this IEntityType entityType)
        {
            return entityType.GetAnnotations().Where(a => a.Name.StartsWith("SyncEngineContext_")).Select(a => a.Value.ToString()).ToArray();
        }
    }
}

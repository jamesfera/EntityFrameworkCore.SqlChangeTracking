using EntityFrameworkCore.SqlChangeTracking.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal;

namespace EntityFrameworkCore.SqlChangeTracking.Migrations
{
    public class SqlChangeTrackingRelationalAnnotationProvider(RelationalAnnotationProviderDependencies dependencies)
        : SqlServerAnnotationProvider(dependencies)
    {
        //public override IEnumerable<IAnnotation> For(IRelationalModel model, bool designTime)
        //{
        //    return base.For(model, designTime).Concat(model.Model.GetAnnotations().Where(a => a.Name.StartsWith(SqlChangeTrackingAnnotationNames.Prefix)));
        //}

        //public override IEnumerable<IAnnotation> For(ITable table, bool designTime)
        //{
        //    var annotations = base.For(table, designTime);

        //    var entityType = (IEntityType)table.EntityTypeMappings.First().TypeBase;

        //    if (entityType.IsChangeTrackingEnabled())
        //        annotations = annotations.Concat(new[] { new Annotation(SqlChangeTrackingAnnotationNames.Enabled, true),
        //            new Annotation(SqlChangeTrackingAnnotationNames.TrackColumns, entityType.ChangeTrackingTrackColumns()) });

        //    return annotations;
        //}
    }
}

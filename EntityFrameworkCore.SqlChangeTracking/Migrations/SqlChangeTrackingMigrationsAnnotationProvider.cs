using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal;

namespace EntityFrameworkCore.SqlChangeTracking.Migrations
{
    public class SqlChangeTrackingRelationalAnnotationProvider : SqlServerAnnotationProvider
    {
        public override IEnumerable<IAnnotation> For(IRelationalModel model, bool designTime)
        {
            return base.For(model, designTime).Concat(model.Model.GetAnnotations().Where(a => a.Name.StartsWith(SqlChangeTrackingAnnotationNames.Prefix)));
        }

        public override IEnumerable<IAnnotation> For(ITable table, bool designTime)
        {
            return base.For(table, designTime).Concat(table.EntityTypeMappings.First().EntityType.GetAnnotations().Where(a => a.Name.StartsWith(SqlChangeTrackingAnnotationNames.Prefix)));
        }
        public SqlChangeTrackingRelationalAnnotationProvider(RelationalAnnotationProviderDependencies dependencies) : base(dependencies) { }
    }
}

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
        public override IEnumerable<IAnnotation> For(IRelationalModel model)
        {
            return base.For(model).Concat(model.Model.GetAnnotations().Where(a => a.Name.StartsWith(SqlChangeTrackingAnnotationNames.Prefix)));
        }

        public override IEnumerable<IAnnotation> For(ITable table)
        {
            return base.For(table).Concat(table.EntityTypeMappings.First().EntityType.GetAnnotations().Where(a => a.Name.StartsWith(SqlChangeTrackingAnnotationNames.Prefix)));
        }
        public SqlChangeTrackingRelationalAnnotationProvider(RelationalAnnotationProviderDependencies dependencies) : base(dependencies) { }
    }
}

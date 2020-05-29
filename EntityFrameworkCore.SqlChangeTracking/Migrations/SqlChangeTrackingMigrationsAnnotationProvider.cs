using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.SqlServer.Migrations.Internal;

namespace EntityFrameworkCore.SqlChangeTracking.Migrations
{
    public class SqlChangeTrackingMigrationsAnnotationProvider : MigrationsAnnotationProvider
    {
        public SqlChangeTrackingMigrationsAnnotationProvider(MigrationsAnnotationProviderDependencies dependencies) : base(dependencies)
        {
            int j = 0;
        }

        public override IEnumerable<IAnnotation> ForRemove(IRelationalModel model)
        {
            return base.ForRemove(model).Concat(model.GetAnnotations().Where(a => a.Name.StartsWith(SqlChangeTrackingAnnotationNames.Prefix)));
        }

        public override IEnumerable<IAnnotation> ForRemove(ITable table)
        {
            return base.ForRemove(table).Concat(table.GetAnnotations().Where(a => a.Name.StartsWith(SqlChangeTrackingAnnotationNames.Prefix)));
        }
    }

    public class SqlChangeTrackingRelationalAnnotationProvider : RelationalAnnotationProvider
    {
        public override IEnumerable<IAnnotation> For(IRelationalModel model)
        {
            return base.For(model).Concat(model.Model.GetAnnotations().Where(a => a.Name.StartsWith(SqlChangeTrackingAnnotationNames.Prefix)));
        }

        public override IEnumerable<IAnnotation> For(ITable table)
        {
            return base.For(table).Concat(table.EntityTypeMappings.First().EntityType.GetAnnotations().Where(a => a.Name.StartsWith(SqlChangeTrackingAnnotationNames.Prefix)));
        }
        public SqlChangeTrackingRelationalAnnotationProvider(RelationalAnnotationProviderDependencies dependencies) : base(dependencies)
        {
        }
    }
}

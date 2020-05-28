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
    public class SqlChangeTrackingMigrationsAnnotationProvider : SqlServerMigrationsAnnotationProvider
    {
        public SqlChangeTrackingMigrationsAnnotationProvider(MigrationsAnnotationProviderDependencies dependencies) : base(dependencies) { }

        
        //public override IEnumerable<IAnnotation> For(IEntityType entityType)
        //{
        //    return base.For(entityType).Concat(entityType.GetAnnotations().Where(a => a.Name.StartsWith(SqlChangeTrackingAnnotationNames.Prefix)));
        //}
        //public override IEnumerable<IAnnotation> For(IModel model)
        //{
        //    return base.For(model).Concat(model.GetAnnotations().Where(a => a.Name.StartsWith(SqlChangeTrackingAnnotationNames.Prefix)));
        //}
    }
}

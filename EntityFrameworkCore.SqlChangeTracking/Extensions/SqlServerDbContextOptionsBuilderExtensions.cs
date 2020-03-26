using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityFrameworkCore.SqlChangeTracking.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.SqlServer.Migrations.Internal;
using Microsoft.Extensions.DependencyInjection;


namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class SqlServerDbContextOptionsBuilderExtensions
    {
        public static SqlServerDbContextOptionsBuilder EnableSqlChangeTracking(this SqlServerDbContextOptionsBuilder sqlBuilder)
        {
            var builder = ((IRelationalDbContextOptionsBuilderInfrastructure) sqlBuilder).OptionsBuilder;

            //((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(extension);

            var extensions = builder.Options.Extensions.ToList();

            builder.ReplaceService<IMigrationsSqlGenerator, SqlChangeTrackingMigrationsSqlGenerator>();
            builder.ReplaceService<IMigrationsAnnotationProvider, SqlChangeTrackingMigrationsAnnotationProvider>();

            return sqlBuilder;
        }
    }
}

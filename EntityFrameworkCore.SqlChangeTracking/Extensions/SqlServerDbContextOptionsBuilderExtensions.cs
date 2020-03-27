using System;
using EntityFrameworkCore.SqlChangeTracking.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;


namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class SqlServerDbContextOptionsBuilderExtensions
    {
        public static SqlServerDbContextOptionsBuilder EnableSqlChangeTracking(this SqlServerDbContextOptionsBuilder sqlBuilder)
        {
            var builder = ((IRelationalDbContextOptionsBuilderInfrastructure) sqlBuilder).OptionsBuilder;

            //((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(extension);

            //var extensions = builder.Options.Extensions.ToList();

            builder.ReplaceService<IMigrationsSqlGenerator, SqlChangeTrackingMigrationsSqlGenerator>();
            builder.ReplaceService<IMigrationsAnnotationProvider, SqlChangeTrackingMigrationsAnnotationProvider>();
            //builder.ReplaceService<IMigrationsModelDiffer, SqlChangeTrackingMigrationsModelDiffer>();
            //builder.ReplaceService<ICSharpMigrationOperationGenerator, SqlChangeTrackingMigrationOperationGenerator>();

            return sqlBuilder;
        }
    }
}

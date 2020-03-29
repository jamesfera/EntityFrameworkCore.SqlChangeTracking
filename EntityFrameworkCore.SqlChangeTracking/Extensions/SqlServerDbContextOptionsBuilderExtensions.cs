using System;
using System.Collections.Generic;
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

            var coreOptions = builder.Options.GetExtension<CoreOptionsExtension>();

            if (coreOptions.InternalServiceProvider == null)
            {
                builder.ReplaceService<IMigrationsSqlGenerator, SqlChangeTrackingMigrationsSqlGenerator>();
                builder.ReplaceService<IMigrationsAnnotationProvider, SqlChangeTrackingMigrationsAnnotationProvider>();
                //builder.ReplaceService<IMigrationsModelDiffer, SqlChangeTrackingMigrationsModelDiffer>();
                //builder.ReplaceService<ICSharpMigrationOperationGenerator, SqlChangeTrackingMigrationOperationGenerator>();
            }
            //var extensions = builder.Options.Extensions.ToList();



            //((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(new ChangeTrackingOptionsExtension());

            return sqlBuilder;
        }

       
    }
}

using System;
using EntityFrameworkCore.SqlChangeTracking.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.SqlServer.Update.Internal;


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
                builder.ReplaceService<IRelationalAnnotationProvider, SqlChangeTrackingRelationalAnnotationProvider>();
                //builder.ReplaceService<ISqlServerUpdateSqlGenerator, SqlChangeTrackingUpdateSqlGenerator>();

                //builder.ReplaceService<IMigrationsModelDiffer, SqlChangeTrackingMigrationsModelDiffer>();
                //builder.ReplaceService<ICSharpMigrationOperationGenerator, SqlChangeTrackingMigrationOperationGenerator>();
            }
            //var extensions = builder.Options.Extensions.ToList();

            //builder.AddInterceptors(new inter());

            //((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(new ChangeTrackingOptionsExtension());

            return sqlBuilder;
        }
    }
}

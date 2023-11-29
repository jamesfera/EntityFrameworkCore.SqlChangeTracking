using EntityFrameworkCore.SqlChangeTracking.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;


namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class SqlServerDbContextOptionsBuilderExtensions
    {
        public static SqlServerDbContextOptionsBuilder EnableSqlChangeTrackingMigrations(this SqlServerDbContextOptionsBuilder sqlBuilder)
        {
            var builder = ((IRelationalDbContextOptionsBuilderInfrastructure)sqlBuilder).OptionsBuilder;

            if (builder.Options.GetExtension<CoreOptionsExtension>().InternalServiceProvider == null)
            {
                builder.ReplaceService<IMigrationsSqlGenerator, SqlChangeTrackingMigrationsSqlGenerator>();
                builder.ReplaceService<IRelationalAnnotationProvider, SqlChangeTrackingRelationalAnnotationProvider>();
            }

            return sqlBuilder;
        }
    }
}

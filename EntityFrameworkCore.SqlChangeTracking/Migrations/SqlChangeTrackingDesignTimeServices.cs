using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.SqlServer.Design.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EntityFrameworkCore.SqlChangeTracking.Migrations
{
    public class SqlChangeTrackingDesignTimeServices : IDesignTimeServices
    {
        public void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
        {
            new SqlServerDesignTimeServices().ConfigureDesignTimeServices(serviceCollection);

            serviceCollection
                .Replace(new ServiceDescriptor(typeof(ICSharpMigrationOperationGenerator), typeof(SqlChangeTrackingMigrationOperationGenerator), ServiceLifetime.Singleton));

            serviceCollection
                .Replace(new ServiceDescriptor(typeof(IMigrationsModelDiffer), typeof(SqlChangeTrackingMigrationsModelDiffer), ServiceLifetime.Scoped));

            serviceCollection
                .Replace(new ServiceDescriptor(typeof(IMigrationsCodeGenerator), typeof(SqlChangeTrackingMigrationsCodeGenerator), ServiceLifetime.Scoped));
        }
    }
}

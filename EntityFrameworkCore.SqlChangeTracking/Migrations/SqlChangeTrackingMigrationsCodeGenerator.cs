using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EntityFrameworkCore.SqlChangeTracking.Migrations
{
    public class SqlChangeTrackingMigrationsCodeGenerator(MigrationsCodeGeneratorDependencies codeDependencies,
        CSharpMigrationsGeneratorDependencies dependencies) 
        : CSharpMigrationsGenerator(codeDependencies, dependencies)
    {
        protected override IEnumerable<string> GetNamespaces(IEnumerable<MigrationOperation> operations)
        {
            return base.GetNamespaces(operations).Concat(new []{ "EntityFrameworkCore.SqlChangeTracking" });
        }
    }
}

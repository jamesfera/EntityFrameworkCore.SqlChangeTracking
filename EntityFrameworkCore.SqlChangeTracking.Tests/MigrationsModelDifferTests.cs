using System;
using System.Collections.Generic;
using System.Text;
using EntityFrameworkCore.SqlChangeTracking.Migrations;
using EntityFrameworkCore.SqlChangeTracking.Migrations.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore.TestUtilities.FakeProvider;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EntityFrameworkCore.SqlChangeTracking.Tests
{
    public class MigrationsModelDifferTests : MigrationsModelDifferTestBase
    {
        class ModelDiffEntity
        {
            public int Id { get; set; }
        }

        protected override IMigrationsModelDiffer CreateModelDiffer(DbContextOptions options)
        {
            var context = TestHelpers.CreateContext(options);

            var serviceProvider = TestHelpers.CreateContextServices();

            return  new SqlChangeTrackingMigrationsModelDiffer(
                new TestRelationalTypeMappingSource(
                    TestServiceFactory.Instance.Create<TypeMappingSourceDependencies>(),
                    TestServiceFactory.Instance.Create<RelationalTypeMappingSourceDependencies>()),
                new SqlChangeTrackingMigrationsAnnotationProvider(new MigrationsAnnotationProviderDependencies()),
                context.GetService<IChangeDetector>(),
                context.GetService<IUpdateAdapterFactory>(),
                context.GetService<CommandBatchPreparerDependencies>());
         
        }

        //see for examples:
        //https://github.com/dotnet/efcore/blob/v3.1.2/test/EFCore.SqlServer.Tests/Migrations/SqlServerModelDifferTest.cs

        [Fact]
        public void OperationIsAddedWhenChangeTrackingIsEnableForTable()
        {
            Execute(
                _ => { },
                source => source.Entity<ModelDiffEntity>(),
                target => target.Entity<ModelDiffEntity>().WithSqlChangeTracking(),
                upOps =>
                {
                    Assert.Equal(1, upOps.Count);

                    var operation = upOps[0];

                    var changeOperation = Assert.IsType<EnableChangeTrackingForTableOperation>(operation);

                    Assert.False(changeOperation.TrackColumns);
                },
                downOps =>
                {

                });
        }

        protected override TestHelpers TestHelpers { get; } = new RelationalTestHelpers(s =>
        {
            s.AddScoped<IMigrationsAnnotationProvider, SqlChangeTrackingMigrationsAnnotationProvider>();
            s.AddScoped<IMigrationsModelDiffer, SqlChangeTrackingMigrationsModelDiffer>();
        });
    }
}

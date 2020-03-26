using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EntityFrameworkCore.SqlChangeTracking.Tests
{
    public static class services
    {
        private static IServiceProvider serviceProvider;
        static services()
        {
            serviceProvider = new ServiceCollection()
                // You can also use InMemory or any other provider here to get the provider-specific conventions
                .AddEntityFrameworkInMemoryDatabase()
                .AddDbContext<TestContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()))
                .BuildServiceProvider();

        }

        public static ModelBuilder GetModelBuilder()
        {
            using var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();

            using var context = serviceScope.ServiceProvider.GetService<TestContext>();

            var conventionSet = ConventionSet.CreateConventionSet(context);
            return new ModelBuilder(conventionSet);
        }
    }

    public class TestContext : DbContext
    {
        public bool ColumnTracking { get; }

        public TestContext(bool columnTracking = false)
        {
            ColumnTracking = columnTracking;
        }

        public DbSet<TestEntity> Entities { get; set;}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //base.OnConfiguring(optionsBuilder);

            optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //base.OnModelCreating(modelBuilder);

            //modelBuilder.Entity<TestEntity>().WithSqlChangeTracking(ColumnTracking);
        }

        public class TestEntity
        {
            public int Id { get; set; }
        }
    }



    public class EntityTypeBuilderTests
    {
        [Fact]
        public void MetadataIsProperlySet_WhenTrackColumnsOff()
        {
            var builder = services.GetModelBuilder();

            builder.Entity<TestContext.TestEntity>().WithSqlChangeTracking(false);

            var model = builder.FinalizeModel();

            var testEntityType = model.FindEntityType(typeof(TestContext.TestEntity));

            Assert.True(testEntityType.IsSqlChangeTrackingEnabled());
            Assert.False(testEntityType.GetTrackColumns());
        }

        [Fact]
        public void MetadataIsProperlySet_WhenTrackColumnsOn()
        {
            var builder = services.GetModelBuilder();

            builder.Entity<TestContext.TestEntity>().WithSqlChangeTracking(true);

            var model = builder.FinalizeModel();

            var testEntityType = model.FindEntityType(typeof(TestContext.TestEntity));

            Assert.True(testEntityType.IsSqlChangeTrackingEnabled());
            Assert.True(testEntityType.GetTrackColumns());
        }
    }
}

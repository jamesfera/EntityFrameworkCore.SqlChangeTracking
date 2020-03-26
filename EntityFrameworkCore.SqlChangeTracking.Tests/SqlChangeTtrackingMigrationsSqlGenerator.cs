using System;
using System.Linq;
using EntityFrameworkCore.SqlChangeTracking.Migrations;
using EntityFrameworkCore.SqlChangeTracking.Migrations.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EntityFrameworkCore.SqlChangeTracking.Tests
{
    public abstract class MigrationSqlGeneratorTestBase
    {
        protected static string EOL => Environment.NewLine;
        protected virtual string Sql { get; set; }
        protected abstract TestHelpers TestHelpers { get; }

        protected virtual void Generate(params MigrationOperation[] operation)
            => Generate(_ => { }, operation);

        protected virtual void Generate(Action<ModelBuilder> buildAction, params MigrationOperation[] operation)
        {
            var modelBuilder = TestHelpers.CreateConventionBuilder();
            modelBuilder.Model.RemoveAnnotation(CoreAnnotationNames.ProductVersion);
            buildAction(modelBuilder);

            var batch = TestHelpers.CreateContextServices().GetRequiredService<IMigrationsSqlGenerator>()
                .Generate(operation, modelBuilder.Model);

            Sql = string.Join(
                "GO" + EOL + EOL,
                batch.Select(b => b.CommandText));
        }

        protected void AssertSql(string expected)
            => Assert.Equal(expected, Sql, ignoreLineEndingDifferences: true);
    }


    public class SqlChangeTrackingMigrationsSqlGeneratorTests : MigrationSqlGeneratorTestBase
    {
        protected override TestHelpers TestHelpers { get; } = new SqlGeneratorTestHelper();

        class SqlGeneratorTestHelper : RelationalTestHelpers
        {
            public override IServiceCollection AddProviderServices(IServiceCollection services)
            {
                services.AddScoped<IMigrationsSqlGenerator, SqlChangeTrackingMigrationsSqlGenerator>();
                return base.AddProviderServices(services);
            }
        }

        [Fact]
        public void ChangeTrackingEnabledOnDatabase()
        {
            var builder = new MigrationBuilder("");

            var retention = 2;
            var autoCleanUp = true;
            Func<bool, string> autoCleanupString = c => c ? "ON" : "OFF";

            builder.Operations.Add(new EnableChangeTrackingForDatabaseOperation(retention, autoCleanUp));

            Generate(builder.Operations.ToArray());

            AssertSql($@"ALTER DATABASE ""Fake Database"" SET CHANGE_TRACKING = ON (CHANGE_RETENTION = {retention} DAYS, AUTO_CLEANUP = {autoCleanupString(autoCleanUp)});{Environment.NewLine}");

            builder = new MigrationBuilder("");

            retention = 5;
            autoCleanUp = false;

            builder.Operations.Add(new EnableChangeTrackingForDatabaseOperation(retention, autoCleanUp));

            Generate(builder.Operations.ToArray());

            AssertSql($@"ALTER DATABASE ""Fake Database"" SET CHANGE_TRACKING = ON (CHANGE_RETENTION = {retention} DAYS, AUTO_CLEANUP = {autoCleanupString(autoCleanUp)});{Environment.NewLine}");
        }
    }
}

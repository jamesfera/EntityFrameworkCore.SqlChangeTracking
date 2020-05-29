using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityFrameworkCore.SqlChangeTracking.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.SqlServer.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EntityFrameworkCore.SqlChangeTracking.Tests
{
    public class MigrationsTests : MigrationsModelDifferTestBase
    {
        class ModelDiffEntity
        {
            public int Id { get; set; }
        }

        public string DatabaseName { get; } = "Fake Database";

        protected static string EOL => Environment.NewLine;
        protected virtual string Sql { get; set; }
        protected virtual void Generate(params MigrationOperation[] operation)
            => Generate(_ => { }, operation);

        protected virtual void Generate(Action<ModelBuilder> buildAction, params MigrationOperation[] operation)
        {
            var modelBuilder = TestHelpers.CreateConventionBuilder();
            modelBuilder.Model.RemoveAnnotation(CoreAnnotationNames.ProductVersion);
            buildAction(modelBuilder);

            var batch = TestHelpers.CreateContextServices().GetRequiredService<IMigrationsSqlGenerator>()
                .Generate(operation, modelBuilder.Model.FinalizeModel());

            Sql = string.Join(
                "GO" + EOL + EOL,
                batch.Select(b => b.CommandText));
        }

        protected void AssertSql(string expected)
            => Assert.Equal(expected, Sql, ignoreLineEndingDifferences: true);

        protected override MigrationsModelDiffer CreateModelDiffer(DbContextOptions options)
        {
            var context = TestHelpers.CreateContext(options);

            //var serviceProvider = TestHelpers.CreateContextServices();

            //return new SqlChangeTrackingMigrationsModelDiffer(
            //    new TestRelationalTypeMappingSource(
            //        TestServiceFactory.Instance.Create<TypeMappingSourceDependencies>(),
            //        TestServiceFactory.Instance.Create<RelationalTypeMappingSourceDependencies>()),
            //    new SqlChangeTrackingMigrationsAnnotationProvider(new MigrationsAnnotationProviderDependencies()),
            //    context.GetService<IChangeDetector>(),
            //    context.GetService<IUpdateAdapterFactory>(),
            //    context.GetService<CommandBatchPreparerDependencies>());

            return new MigrationsModelDiffer(
                new TestRelationalTypeMappingSource(
                    TestServiceFactory.Instance.Create<TypeMappingSourceDependencies>(),
                    TestServiceFactory.Instance.Create<RelationalTypeMappingSourceDependencies>()),
                new SqlServerMigrationsAnnotationProvider(new MigrationsAnnotationProviderDependencies()),
                context.GetService<IChangeDetector>(),
                context.GetService<IUpdateAdapterFactory>(),
                context.GetService<CommandBatchPreparerDependencies>());

        }

        protected override TestHelpers TestHelpers { get; } = new RelationalTestHelpers(s =>
        {
            s.AddScoped<IRelationalAnnotationProvider, SqlChangeTrackingRelationalAnnotationProvider>();
            //s.AddScoped<IMigrationsModelDiffer, SqlChangeTrackingMigrationsModelDiffer>();
            s.AddScoped<IMigrationsSqlGenerator, SqlChangeTrackingMigrationsSqlGenerator>();
        });

        //see for examples:
        //https://github.com/dotnet/efcore/blob/v3.1.2/test/EFCore.SqlServer.Tests/Migrations/SqlServerModelDifferTest.cs

        [Fact]
        public void SqlGeneratedWhenChangeTrackingEnabledForDatabase()
        {
            Execute(
                _ => { },
                source => { },
                target => target.ConfigureChangeTracking(c =>
                {
                    c.EnableSnapshotIsolation = false;
                    c.RetentionDays = 5;
                    c.AutoCleanUp = false;
                }),
                upOps =>
                {
                    var migrationOperation = Assert.Single(upOps);

                    Generate(migrationOperation);

                    AssertSql($@"ALTER DATABASE ""{DatabaseName}"" SET CHANGE_TRACKING = ON (CHANGE_RETENTION = {5} DAYS, AUTO_CLEANUP = OFF);{Environment.NewLine}");
                },
                downOps => { });
        }

        [Fact]
        public void SqlGeneratedWhenChangeTrackingDisabledForDatabase()
        {
            Execute(
                _ => { },
                source => source.ConfigureChangeTracking(),
                target => { },
                upOps =>
                {
                    var migrationOperation = Assert.Single(upOps);

                    Generate(migrationOperation);

                    AssertSql($@"ALTER DATABASE ""{DatabaseName}"" SET CHANGE_TRACKING = OFF;{Environment.NewLine}");
                },
                downOps => { });
        }

        [Fact]
        public void SqlGeneratedWhenSnapshotIsolationEnabled()
        {
            Execute(
                _ => { },
                source => { },
                target => target.Model.EnableSnapshotIsolation(),
                upOps =>
                {
                    var migrationOperation = Assert.Single(upOps);

                    Generate(migrationOperation);

                        //.Append("ALTER DATABASE ")
                        //.Append(sqlHelper.DelimitIdentifier(Dependencies.CurrentContext.Context.Database.GetDbConnection().Database))
                        //.Append(" SET ALLOW_SNAPSHOT_ISOLATION ON ")
                        //.AppendLine(sqlHelper.StatementTerminator)
                        //.EndCommand();

                    AssertSql($@"ALTER DATABASE ""{DatabaseName}"" SET ALLOW_SNAPSHOT_ISOLATION ON;{Environment.NewLine}");
                },
                downOps => { });
        }

        [Fact]
        public void SqlGeneratedWhenChangeTrackingEnabledForTable()
        {
            Execute(
                _ => { },
                source => source.Entity<ModelDiffEntity>(),
                target => target.Entity<ModelDiffEntity>().WithSqlChangeTracking(),
                upOps =>
                {
                    var migrationOperation = Assert.Single(upOps);

                    Generate(migrationOperation);
                    
                    AssertSql($@"ALTER TABLE ""{nameof(ModelDiffEntity)}"" ENABLE CHANGE_TRACKING;{Environment.NewLine}");
                },
                downOps => { });

            Execute(
                _ => { },
                source => source.Entity<ModelDiffEntity>(),
                target => target.Entity<ModelDiffEntity>().WithSqlChangeTracking(true),
                upOps =>
                {
                    var migrationOperation = Assert.Single(upOps);

                    Generate(migrationOperation);

                    AssertSql($@"ALTER TABLE ""{nameof(ModelDiffEntity)}"" ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON);{Environment.NewLine}");
                },
                downOps => { });
        }

        [Fact]
        public void SqlGeneratedWhenChangeTrackingEnabledForNewTable()
        {
            Execute(
                _ => { },
                source => { },
                target => target.Entity<ModelDiffEntity>().WithSqlChangeTracking(),
                upOps =>
                {
                    Generate(upOps.ToArray());

                    var expectedSql = $@"ALTER TABLE ""{nameof(ModelDiffEntity)}"" ENABLE CHANGE_TRACKING;{Environment.NewLine}";

                    Assert.Contains("CREATE TABLE", Sql);

                    Assert.EndsWith(expectedSql, Sql);
                },
                downOps => { });
        }

        [Fact]
        public void SqlGeneratedWhenChangeTrackingDisabledForTable()
        {
            Execute(
                _ => { },
                source => source.Entity<ModelDiffEntity>().WithSqlChangeTracking(),
                target => target.Entity<ModelDiffEntity>(),
                upOps =>
                {
                    var migrationOperation = Assert.Single(upOps);

                    Generate(migrationOperation);

                    AssertSql($@"ALTER TABLE ""{nameof(ModelDiffEntity)}"" DISABLE CHANGE_TRACKING;{Environment.NewLine}");
                },
                downOps => { });
        }


    }
}

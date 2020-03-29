using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityFrameworkCore.SqlChangeTracking.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace EntityFrameworkCore.SqlChangeTracking.Tests
{
    public class MigrationBuilderExtensionsTests
    {
        [Fact]
        public void EnableChangeTrackingAddsOperationToMigrationBuilder()
        {
            var builder = new MigrationBuilder("");

            var retention = 3;
            var autoCleanUp = false;

            builder.EnableChangeTrackingForDatabase(retention, autoCleanUp);

            Assert.Single(builder.Operations);

            var operation = builder.Operations.First();

            var dbOperation = Assert.IsType<EnableChangeTrackingForDatabaseOperation>(operation);

            Assert.Equal(dbOperation.RetentionDays, retention);
            Assert.Equal(dbOperation.AutoCleanup, autoCleanUp);
        }
    }
}

using System;
using EntityFrameworkCore.SqlChangeTracking.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.SqlServer.Migrations.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine
{
    public static class SqlServerDbContextOptionsBuilderExtensions
    {
        public static SqlServerDbContextOptionsBuilder EnableSyncEngine(this SqlServerDbContextOptionsBuilder sqlBuilder)
        {
            sqlBuilder.EnableSqlChangeTracking();

            var builder = ((IRelationalDbContextOptionsBuilderInfrastructure)sqlBuilder).OptionsBuilder;

            var coreOptions = builder.Options.GetExtension<CoreOptionsExtension>();

            if (coreOptions.InternalServiceProvider == null)
            {

            }

            return sqlBuilder;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkCore.SqlChangeTracking.Migrations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query;


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
                builder.ReplaceService<IMigrationsAnnotationProvider, SqlChangeTrackingMigrationsAnnotationProvider>();
                
                //builder.ReplaceService<IQuerySqlGeneratorFactory, AsOfQuerySqlGeneratorFactory>();
                //builder.ReplaceService<IQueryableMethodTranslatingExpressionVisitorFactory, AsOfQueryableMethodTranslatingExpressionVisitorFactory>();
                //builder.ReplaceService<ISqlExpressionFactory, AsOfSqlExpressionFactory>();

                //builder.ReplaceService<IMigrationsModelDiffer, SqlChangeTrackingMigrationsModelDiffer>();
                //builder.ReplaceService<ICSharpMigrationOperationGenerator, SqlChangeTrackingMigrationOperationGenerator>();
            }
            //var extensions = builder.Options.Extensions.ToList();

            builder.AddInterceptors(new inter());

            //((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(new ChangeTrackingOptionsExtension());

            return sqlBuilder;
        }

        public class inter : DbCommandInterceptor
        {
            public override Task<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = new CancellationToken())
            {
                int j = 0;
                
                return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
            }

            public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
            {
                int j = 0;
                return base.NonQueryExecuting(command, eventData, result);
            }

            public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
            {
                int j = 0;
                return base.ScalarExecuting(command, eventData, result);
            }

            public override Task<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = new CancellationToken())
            {
                int j = 0;
                return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
            }

            public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
            {
                int j = 0;
                return base.ReaderExecuting(command, eventData, result);
            }

            public override Task<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = new CancellationToken())
            {
                if (TrackingContextAsyncLocalCache.CurrentTrackingContext != null)
                {
                    StringBuilder commandBuilder = new StringBuilder(command.CommandText);

                    bool noCount = false;

                    if (command.CommandText.Contains("SET NOCOUNT ON;"))
                    {
                        noCount = true;
                        commandBuilder.Replace("SET NOCOUNT ON;", "");
                    }

                    commandBuilder.Insert(0, $"DECLARE @dc varbinary(128) = CONVERT(VARBINARY(128), '{TrackingContextAsyncLocalCache.CurrentTrackingContext}');\nWITH CHANGE_TRACKING_CONTEXT( @dc )");

                    if(noCount)
                        commandBuilder.Insert(0, "SET NOCOUNT ON;\n");

                    command.CommandText = commandBuilder.ToString();
                }

                return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
            }
        }

       
    }
}

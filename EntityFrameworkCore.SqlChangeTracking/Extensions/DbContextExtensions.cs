using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class DbContextExtensions
    {
        public static bool IsSnapshotIsolationEnabled<TContext>(this TContext dbContext) where TContext : DbContext
        {
            var sql =
                $"SELECT snapshot_isolation_state_desc from sys.databases where name = '{dbContext.Database.GetDbConnection().Database}'";

            var result = dbContext.SqlQuery(() => new {snapshot_isolation_state_desc = ""}, sql).FirstOrDefault();

            return result?.snapshot_isolation_state_desc == "ON";
        }
    }

    internal static class InternalDbContextExtensions
    { 
        public static IList<T> SqlQuery<T>(this DbContext db, Func<T> targetType, string sql, params object[] parameters) where T : class
        {
            return SqlQuery<T>(db, sql, parameters);
        }
        public static IList<T> SqlQuery<T>(this DbContext db, string sql, params object[] parameters) where T : class
        {
            using var db2 = new ContextForQueryType<T>(db.Database.GetDbConnection());
            
            return db2.Set<T>().FromSqlRaw(sql, parameters).ToList();
        }

        class ContextForQueryType<T> : DbContext where T : class
        {
            DbConnection con;

            public ContextForQueryType(DbConnection con)
            {
                this.con = con;
            }
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                //switch on the connection type name to enable support multiple providers
                //var name = con.GetType().Name;

                optionsBuilder.UseSqlServer(con);

                base.OnConfiguring(optionsBuilder);
            }
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                var t = modelBuilder.Entity<T>().HasNoKey();

                //to support anonymous types, configure entity properties for read-only properties
                foreach (var prop in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (prop.CustomAttributes.All(a => a.AttributeType != typeof(NotMappedAttribute)))
                        t.Property(prop.Name);
                }

                base.OnModelCreating(modelBuilder);
            }
        }
    }
}

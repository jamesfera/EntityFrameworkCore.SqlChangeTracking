using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkCore.SqlChangeTracking
{
    public static class DbContextExtensions
    {
        

        public static IList<KeyValuePair<string, object>> GetLogContext<TContext>(this TContext dbContext) where TContext : DbContext
        {
            return new List<KeyValuePair<string, object>>()
            {
                new KeyValuePair<string, object>("DbContextId", dbContext.ContextId),
                new KeyValuePair<string, object>("DatabaseName", dbContext.Database.GetDbConnection().Database),
                new KeyValuePair<string, object>("DbContextType", dbContext.GetType()),
            };
        }


    }
    
    internal static class InternalDbContextExtensions
    {
        public static Task<List<T>> SqlQueryAsync<T>(this DbContext db, Func<T> targetType, string sql, params object[] parameters) where T : class
        {
            using var db2 = new ContextForQueryType<T>(db.Database.GetDbConnection());

            return db2.Set<T>().FromSqlRaw(sql, parameters).ToListAsync();
        }
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

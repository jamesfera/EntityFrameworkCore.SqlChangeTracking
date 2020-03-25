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
                var type = typeof(T);

                addTypeToModel(type, modelBuilder);

                //if (type.IsGenericType)
                //{
                //    foreach (var genericArgument in type.GetGenericArguments())
                //    {
                //        addTypeToModel(genericArgument, modelBuilder);
                //    }
                //}

                base.OnModelCreating(modelBuilder);
            }

            private void addTypeToModel(Type typeToAdd, ModelBuilder modelBuilder)
            {
                var t = modelBuilder.Entity(typeToAdd).HasNoKey();

                //to support anonymous types, configure entity properties for read-only properties
                foreach (var prop in typeToAdd.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (prop.CustomAttributes.All(a => a.AttributeType != typeof(NotMappedAttribute)))
                        t.Property(prop.Name);
                }
            }
        }
    }
}

using System.Collections.Generic;
using EntityFrameworkCore.SqlChangeTracking.Extensions.Internal;
using EntityFrameworkCore.SqlChangeTracking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EntityFrameworkCore.SqlChangeTracking.Extensions
{

    public interface IChangesQueryContext<T> where T : class
    {
        DbSet<T> DbSet { get; }
        IAsyncEnumerable<IChangeTrackingEntry<T>> Next(long lastVersion);
        IAsyncEnumerable<IChangeTrackingEntry<T>> All(long lastVersion);
    }

    internal class ChangesQueryContext<T> : IChangesQueryContext<T> where T : class, new()
    {
        public DbSet<T> DbSet { get; }

        public ChangesQueryContext(DbSet<T> dbSet)
        {
            DbSet = dbSet;
        }

        public IAsyncEnumerable<IChangeTrackingEntry<T>> Next(long lastVersion)
        {
            var dbContext = DbSet.GetService<ICurrentDbContext>().Context;

            var entityType = dbContext.Model.FindEntityType(typeof(T));

            return dbContext.Next<T>(entityType, lastVersion);
        }

        public IAsyncEnumerable<IChangeTrackingEntry<T>> All(long lastVersion)
        {
            var dbContext = DbSet.GetService<ICurrentDbContext>().Context;

            var entityType = dbContext.Model.FindEntityType(typeof(T));

            return dbContext.All<T>(entityType, lastVersion);
        }
    }

}

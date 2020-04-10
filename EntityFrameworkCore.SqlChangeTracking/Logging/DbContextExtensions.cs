using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.SqlChangeTracking.Logging
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
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EntityFrameworkCore.SqlChangeTracking
{
    internal static class TrackingContextAsyncLocalCache
    {
        static readonly AsyncLocal<ConcurrentDictionary<string, string>> AsyncLocalContext = new AsyncLocal<ConcurrentDictionary<string, string>>();

        public static string? GetTrackingContextForTable(string tableName)
        {
            if (AsyncLocalContext.Value.TryGetValue(tableName, out string value))
                return value;

            return null;
        }

        public class ChangeTrackingContext : IDisposable
        {
            readonly string _table;

            public ChangeTrackingContext(string table, string context)
            {
                if(string.IsNullOrWhiteSpace(context))
                    return;

                _table = table;

               (AsyncLocalContext.Value ?? (AsyncLocalContext.Value = new ConcurrentDictionary<string, string>())).TryAdd(table, context);
            }

            public void Dispose()
            {
                AsyncLocalContext.Value?.TryRemove(_table, out string value);
            }
        }
    }
    
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EntityFrameworkCore.SqlChangeTracking
{
    public interface IChangeTrackingContext : IDisposable
    {
        string TrackingContext { get; }
    }

    internal static class TrackingContextAsyncLocalCache
    {
        static readonly AsyncLocal<ChangeTrackingContext?> AsyncLocalContext = new AsyncLocal<ChangeTrackingContext?>();

        public static string CurrentTrackingContext => AsyncLocalContext.Value?.TrackingContext;

        public class ChangeTrackingContext : IChangeTrackingContext
        {
            public ChangeTrackingContext(string context)
            {
                if(string.IsNullOrWhiteSpace(context))
                    return;

                TrackingContext = context;
                AsyncLocalContext.Value = this;
            }

            public void Dispose()
            {
                AsyncLocalContext.Value = null;
                //return new ValueTask();
            }

            public string TrackingContext { get; }
        }
    }
    
}

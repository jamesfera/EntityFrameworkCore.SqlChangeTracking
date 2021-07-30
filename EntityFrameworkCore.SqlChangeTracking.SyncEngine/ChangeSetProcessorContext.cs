using System;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine
{
    public interface IChangeSetProcessorContext<TContext> where TContext : DbContext
    {
        TContext DbContext { get; }
        string SyncContext { get; }
        bool RecordCurrentVersion { get; }
        void Skip();
        int Skipped { get; }
        void Dispose();
        void SkipRecordCurrentVersion();
    }

    internal class ChangeSetProcessorContext<TContext> : IDisposable, IChangeSetProcessorContext<TContext> where TContext : DbContext
    {
        internal ChangeSetProcessorContext(TContext dbContext, string syncContext)
        {
            DbContext = dbContext;
            SyncContext = syncContext;
        }

        public int Skipped { get; private set; } = 0;

        public void Skip()
        {
            Skipped++;
        }

        public TContext DbContext { get; }
        public string SyncContext { get; }
        public bool RecordCurrentVersion { get; internal set; } = true;

        public void SkipRecordCurrentVersion()
        {
            RecordCurrentVersion = false;
        }

        public void Dispose()
        {
        }
    }
}
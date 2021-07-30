using System;
using System.Threading;
using System.Threading.Tasks;
using EntityFrameworkCore.SqlChangeTracking.SyncEngine.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine
{
    public class SyncEngineHostedService : IHostedService
    {
        readonly ISyncEngine _syncEngine;

        public SyncEngineHostedService(ISyncEngine syncEngine)
        {
            _syncEngine = syncEngine;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _syncEngine.Start(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _syncEngine.Stop(cancellationToken);
        }
    }
}

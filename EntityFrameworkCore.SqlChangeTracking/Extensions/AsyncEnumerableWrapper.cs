using System.Collections.Generic;
using System.Threading;
using EntityFrameworkCore.SqlChangeTracking.Models;

namespace EntityFrameworkCore.SqlChangeTracking
{
    public class AsyncEnumerableWrapper<T> : IAsyncEnumerable<IChangeTrackingEntry<T>> where T : class
    {
        IAsyncEnumerable<IChangeTrackingEntry<T>> _enumerable;
        string _sql;

        public AsyncEnumerableWrapper(IAsyncEnumerable<IChangeTrackingEntry<T>> enumerable, string sql)
        {
            _enumerable = enumerable;
            _sql = sql;
        }

        public IAsyncEnumerator<IChangeTrackingEntry<T>> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            return _enumerable.GetAsyncEnumerator(cancellationToken);
        }

        public override string ToString()
        {
            return _sql;
        }
    }
}

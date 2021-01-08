using System.Threading.Tasks;
using EntityFrameworkCore.SqlChangeTracking.Extensions.Internal;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.SqlChangeTracking.Extensions
{
    public static class DbContextExtensions
    {
        public static async ValueTask<long?> GetCurrentChangeTrackingVersion(this DbContext dbContext)
        {
            var reader = (await dbContext.Database.ExecuteSqlQueryAsync("SELECT CHANGE_TRACKING_CURRENT_VERSION()").ConfigureAwait(false)).DbDataReader;

            while (await reader.ReadAsync().ConfigureAwait(false))
                return reader[0] as long?;

            return null;
        }
    }
}

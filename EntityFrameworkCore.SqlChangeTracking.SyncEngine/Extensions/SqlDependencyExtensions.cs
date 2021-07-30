using System;
using System.Collections.Generic;
using System.Text;
using EntityFrameworkCore.SqlChangeTracking.Models;
using EntityFrameworkCore.SqlChangeTracking.SyncEngine.Utils;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine.Extensions
{
    public static class SqlDependencyExtensions
    {
        public static ChangeOperation ToChangeOperation(this SqlDependencyEx.NotificationTypes notificationType)
        {
            return notificationType switch
            {
                SqlDependencyEx.NotificationTypes.Insert => ChangeOperation.Insert,
                SqlDependencyEx.NotificationTypes.Update => ChangeOperation.Update,
                SqlDependencyEx.NotificationTypes.Delete => ChangeOperation.Delete,
                SqlDependencyEx.NotificationTypes.None => ChangeOperation.None
            };
        }
    }
}

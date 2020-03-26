using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EntityFrameworkCore.SqlChangeTracking.Migrations.Operations
{
    public class EnableChangeTrackingForDatabaseOperation : MigrationOperation
    {
        public int RetentionDays { get; }
        public bool AutoCleanup { get; }

        public EnableChangeTrackingForDatabaseOperation(int retentionDays = 2, bool autoCleanup = true)
        {
            RetentionDays = retentionDays;
            AutoCleanup = autoCleanup;
        }
    }
}

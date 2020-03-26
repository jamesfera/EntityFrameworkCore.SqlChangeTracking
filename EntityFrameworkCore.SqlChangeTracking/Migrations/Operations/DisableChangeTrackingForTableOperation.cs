using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EntityFrameworkCore.SqlChangeTracking.Migrations.Operations
{
    public class DisableChangeTrackingForTableOperation : MigrationOperation
    {
        public string Name { get; set; }
        public string? Schema { get; set; }

        public DisableChangeTrackingForTableOperation(string name, string? schema = null)
        {
            Name = name;
            Schema = schema;
        }
    }
}

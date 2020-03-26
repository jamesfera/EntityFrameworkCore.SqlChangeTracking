using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EntityFrameworkCore.SqlChangeTracking.Migrations.Operations
{
    public class EnableChangeTrackingForTableOperation : MigrationOperation
    {
        public string Name { get; set; }
        public string? Schema { get; set; }
        public bool TrackColumns { get; set; }

        public EnableChangeTrackingForTableOperation(string name, string? schema = null, bool trackColumns = false)
        {
            Name = name;
            Schema = schema;
            TrackColumns = trackColumns;
        }
    }
}

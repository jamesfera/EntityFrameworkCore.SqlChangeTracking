using EntityFrameworkCore.SqlChangeTracking.Models;

namespace EntityFrameworkCore.SqlChangeTracking.SyncEngine.Monitoring
{
    public interface ITableChangedNotification
    {
        string Database { get; }
        string Schema { get; }
        string Table { get; }
        ChangeOperation ChangeOperation { get; }
    }

    internal class TableChangedNotification : ITableChangedNotification
    {
        public TableChangedNotification(string database, string table, string schema, ChangeOperation changeOperation)
        {
            Database = database;
            Table = table;
            Schema = schema;
            ChangeOperation = changeOperation;
        }

        public string Database { get; }
        public string Schema { get; }
        public string Table { get; }
        public ChangeOperation ChangeOperation { get; }
    }
}
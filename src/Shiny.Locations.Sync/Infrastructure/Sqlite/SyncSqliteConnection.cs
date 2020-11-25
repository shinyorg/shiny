using System;
using System.IO;
using Shiny.IO;
using SQLite;


namespace Shiny.Locations.Sync.Infrastructure.Sqlite
{
    public class SyncSqliteConnection : SQLiteAsyncConnection
    {
        public SyncSqliteConnection(IPlatform platform) : base(Path.Combine(platform.AppData.FullName, "shinylocsync.db"), true)
        {
            var conn = this.GetConnection();
            conn.CreateTable<SqliteGeofenceEvent>();
            conn.CreateTable<SqliteGpsEvent>();
        }
    }
}

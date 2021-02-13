using System;
using System.IO;
using Shiny.Models;
using SQLite;


namespace Shiny.Integrations.Sqlite
{
    public class ShinySqliteConnection : SQLiteAsyncConnection
    {
        public ShinySqliteConnection(IPlatform platform) : base(Path.Combine(platform.AppData.FullName, "shiny.db"))
        {
            var conn = this.GetConnection();
            conn.CreateTable<LogStore>();
            conn.CreateTable<RepoStore>();
            conn.CreateTable<SettingStore>();
        }


        public void Purge()
        {
            var conn = this.GetConnection();
            conn.DeleteAll<LogStore>();
            conn.DeleteAll<RepoStore>();
            conn.DeleteAll<SettingStore>();
        }


        public AsyncTableQuery<LogStore> Logs => this.Table<LogStore>();
        public AsyncTableQuery<RepoStore> RepoItems => this.Table<RepoStore>();
        public AsyncTableQuery<SettingStore> Settings => this.Table<SettingStore>();
    }
}

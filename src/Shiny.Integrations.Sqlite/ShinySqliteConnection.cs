using System;
using System.IO;
using Shiny.IO;
using Shiny.Models;
using SQLite;


namespace Shiny
{
    public class ShinySqliteConnection : SQLiteAsyncConnection
    {
        public ShinySqliteConnection(IFileSystem fileSystem) : base(Path.Combine(fileSystem.AppData.FullName, "shiny.db"))
        {
            var conn = this.GetConnection();
            conn.CreateTable<CacheStore>();
            conn.CreateTable<LogStore>();
            conn.CreateTable<RepoStore>();
            conn.CreateTable<SettingStore>();
        }


        public AsyncTableQuery<CacheStore> Cache => this.Table<CacheStore>();
        public AsyncTableQuery<LogStore> Logs => this.Table<LogStore>();
        public AsyncTableQuery<RepoStore> RepoItems => this.Table<RepoStore>();
        public AsyncTableQuery<SettingStore> Settings => this.Table<SettingStore>();
    }
}

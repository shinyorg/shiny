using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Shiny.IO;
using SQLite;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class SqliteDataService : IDataService
    {
        readonly SQLiteAsyncConnection conn;


        public SqliteDataService(IFileSystem fileSystem)
        {
            this.conn = new SQLiteAsyncConnection(Path.Combine(fileSystem.AppData.FullName, "shinylocsync.db"), true);

            var sync = this.conn.GetConnection();
            sync.CreateTable<GeofenceEvent>();
            sync.CreateTable<GpsEvent>();
        }


        public Task Create<T>(T obj) where T : LocationEvent, new() 
            => this.conn.InsertAsync(obj);

        public Task<List<T>> GetAll<T>() where T : LocationEvent, new()
            => this.conn.Table<T>().ToListAsync();

        public Task<int> GetPendingCount<T>() where T : LocationEvent, new()
            => this.conn.Table<T>().CountAsync();

        public Task Remove<T>(T obj) where T : LocationEvent, new()
            => this.conn.DeleteAsync(obj);
    }
}

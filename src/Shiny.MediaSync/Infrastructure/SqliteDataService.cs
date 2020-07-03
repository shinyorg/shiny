using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Shiny.IO;
using SQLite;


namespace Shiny.MediaSync.Infrastructure
{
    public class SqliteDataService : IDataService
    {
        readonly SQLiteAsyncConnection conn;
        public SqliteDataService(IFileSystem fileSystem)
            => this.conn = new SQLiteAsyncConnection(Path.Combine(fileSystem.AppData.FullName, "shinymedia.db"));


        public Task Create(SyncItem sync) => this.conn.InsertAsync(sync);
        public async Task<IList<SyncItem>> GetAll() => await this.conn.Table<SyncItem>().ToListAsync();
        public Task<SyncItem> GetById(string id) => this.conn.GetAsync<SyncItem>(id);
        public async Task<bool> Remove(string id)
        {
            var count = await this.conn.DeleteAsync<SyncItem>(id);
            return count > 0;
        }
    }
}

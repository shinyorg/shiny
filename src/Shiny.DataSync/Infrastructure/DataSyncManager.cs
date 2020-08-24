using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.IO;
using SQLite;


namespace Shiny.DataSync.Infrastructure
{
    public class DataSyncManager : NotifyPropertyChanged, IDataSyncManager
    {
        readonly SQLiteAsyncConnection conn;
        readonly ISerializer serializer;


        public DataSyncManager(IFileSystem fileSystem, ISerializer serializer)
        {
            this.conn = new SQLiteAsyncConnection(Path.Combine(fileSystem.AppData.FullName, "shinydatasync.db"));
            this.conn.GetConnection().CreateTable<SyncItem>();
            this.serializer = serializer;
        }


        DateTimeOffset? lastSync;
        public DateTimeOffset? LastSync
        {
            get => this.lastSync;
            set => this.Set(ref this.lastSync, value);
        }


        public Task Save<T>(T entity, SyncOperation operation) where T : ISyncEntity => this.conn.InsertAsync(new SyncItem
        {
            Id = Guid.NewGuid(),
            EntityId = entity.Id,
            TypeName = typeof(T).FullName,
            SerializedEntity = this.serializer.Serialize(entity),
            Operation = operation,
            Timestamp = DateTimeOffset.UtcNow
        });

        public Task<List<SyncItem>> GetPendingItems() => this.conn.Table<SyncItem>().ToListAsync();
        public Task Remove(Guid syncItemId) => this.conn.DeleteAsync<SyncItem>(syncItemId);
    }
}

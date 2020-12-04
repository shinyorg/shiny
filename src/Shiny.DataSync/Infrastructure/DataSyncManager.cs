using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;
using SQLite;


namespace Shiny.DataSync.Infrastructure
{
    public class DataSyncManager : NotifyPropertyChanged, IDataSyncManager
    {
        readonly SQLiteAsyncConnection conn;
        readonly ISerializer serializer;
        readonly IJobManager jobManager;


        public DataSyncManager(IPlatform platform,
                               ISerializer serializer,
                               IJobManager jobManager)
        {
            this.conn = new SQLiteAsyncConnection(Path.Combine(platform.AppData.FullName, "shinydatasync.db"));
            this.conn.GetConnection().CreateTable<SyncItem>();
            this.serializer = serializer;
            this.jobManager = jobManager;
        }


        bool enabled;
        public bool Enabled
        {
            get => this.enabled;
            set => this.Set(ref this.enabled, value);
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
            EntityId = entity.EntityId,
            TypeName = typeof(T).FullName,
            SerializedEntity = this.serializer.Serialize(entity),
            Operation = operation,
            Timestamp = DateTimeOffset.UtcNow
        });


        public Task<List<SyncItem>> GetPendingItems() => this
            .conn
            .Table<SyncItem>()
            .OrderBy(x => x.Timestamp)
            .ToListAsync();
        public Task Remove(Guid syncItemId) => this.conn.DeleteAsync<SyncItem>(syncItemId);
        public Task ForceRun() => this.jobManager.RunJobAsTask(SyncJob.JobName);
        public Task ClearPending() => this.conn.DeleteAllAsync<SyncItem>();
    }
}

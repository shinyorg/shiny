using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.DataSync
{
    public interface IDataSyncManager
    {
        Task Save<T>(T entity, SyncOperation operation) where T : ISyncEntity;
        Task<List<SyncItem>> GetPendingItems();
        Task Remove(Guid syncItemId);
        Task ForceRun();
        Task ClearPending();

        DateTimeOffset? LastSync { get; }
        bool Enabled { get; set; }

        //bool ItemsMustSyncBeforeProgressing { get; }
    }
}

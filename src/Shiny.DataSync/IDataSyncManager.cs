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

        DateTimeOffset? LastSync { get; }
    }
}

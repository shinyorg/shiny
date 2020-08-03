using System;
using System.Threading.Tasks;


namespace Shiny.DataSync
{
    public interface IDataSyncManager
    {
        Task Create<T>(T entity) where T : ISyncEntity;
        Task Update<T>(T entity) where T : ISyncEntity;
        Task Delete<T>(T entity) where T : ISyncEntity;

        //Task<int> GetPendingCount();
        DateTimeOffset? LastSync { get; }
    }
}

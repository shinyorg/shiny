using System;
using System.Threading.Tasks;

namespace Shiny.DataSync.Infrastructure
{
    public class DataSyncManager : IDataSyncManager
    {
        public DataSyncManager()
        {
        }

        public DateTimeOffset? LastSync => throw new NotImplementedException();

        public Task Create<T>(T entity) where T : ISyncEntity
        {
            throw new NotImplementedException();
        }

        public Task Delete<T>(T entity) where T : ISyncEntity
        {
            throw new NotImplementedException();
        }

        public Task Update<T>(T entity) where T : ISyncEntity
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Threading.Tasks;


namespace Shiny.DataSync
{
    public interface ISyncManager
    {
        Task Create<T>(T entity) where T : ISyncEntity;
        Task Update<T>(T entity) where T : ISyncEntity;
        Task Delete<T>(T entity) where T : ISyncEntity;
    }
}

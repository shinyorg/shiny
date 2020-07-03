using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.MediaSync.Infrastructure
{
    public interface IDataService
    {
        Task Create(SyncItem sync);
        Task<SyncItem> GetById(string id);
        Task<IList<SyncItem>> GetAll();
    }
}

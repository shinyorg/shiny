using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Locations.Sync.Infrastructure
{
    public interface IDataService
    {
        Task<List<T>> GetAll<T>() where T : LocationEvent, new();
        Task Create<T>(T obj) where T : LocationEvent, new();
        Task Remove<T>(T obj) where T : LocationEvent, new();
        Task<int> GetPendingCount<T>() where T : LocationEvent, new();
    }
}

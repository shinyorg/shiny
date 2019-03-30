using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Infrastructure
{
    public interface IRepository
    {
        Task<T> Get<T>(string key) where T : class;
        Task<List<T>> GetAll<T>() where T : class;
        Task Set(string key, object entity);
        Task<bool> Remove<T>(string key) where T : class;
        Task Clear<T>() where T : class;
        IObservable<RepositoryEvent> WhenEvent();
    }
}

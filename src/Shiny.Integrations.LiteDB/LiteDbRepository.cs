using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny
{
    class LiteDbRepository : IRepository
    {
        readonly ShinyLiteDatabase database;
        public LiteDbRepository(ShinyLiteDatabase database)
            => this.database = database;


        public Task Clear<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public Task<bool> Exists<T>(string key) where T : class
        {
            throw new NotImplementedException();
        }

        public Task<T> Get<T>(string key) where T : class
        {
            throw new NotImplementedException();
        }

        public Task<IList<T>> GetAll<T>() where T : class
        {
            throw new NotImplementedException();
        }


        public Task<IDictionary<string, T>> GetAllWithKeys<T>() where T : class
        {
            throw new NotImplementedException();
        }


        public Task<bool> Remove<T>(string key) where T : class
        {
            throw new NotImplementedException();
        }

        public Task<bool> Set(string key, object entity)
        {
            throw new NotImplementedException();
        }

        public IObservable<RepositoryEvent> WhenEvent()
        {
            throw new NotImplementedException();
        }
    }
}

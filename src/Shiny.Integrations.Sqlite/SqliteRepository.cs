using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny
{
    class SqliteRepository : IRepository
    {
        readonly ShinySqliteConnection conn;
        public SqliteRepository(ShinySqliteConnection conn)
            => this.conn = conn;


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

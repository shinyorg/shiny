using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Shiny.Infrastructure
{
    public class RepositoryWrapper<TArgs, TStore>
            where TArgs: class
            where TStore : class
    {
        readonly Func<TArgs, TStore> toStore;
        readonly Func<TStore, TArgs> fromStore;


        public RepositoryWrapper(IRepository repository,
                                 Func<TArgs, TStore> toStore,
                                 Func<TStore, TArgs> fromStore)
        {
            this.Repository = repository;
            this.toStore = toStore;
            this.fromStore = fromStore;
        }


        public IRepository Repository { get; }
        public Task Set(string identifier, TArgs args) => this.Repository.Set(identifier, this.toStore(args));
        public Task Clear() => this.Repository.Clear<TStore>();
        public Task Remove(string identifier) => this.Repository.Remove<TStore>(identifier);


        public async Task<TArgs?> Get(string identifier)
        {
            var store = await this.Repository
                .Get<TStore>(identifier)
                .ConfigureAwait(false);

            if (store == null)
                return null;

            return this.fromStore(store);
        }


        public async Task<List<TArgs>> GetAll()
        {
            var regions = await this.Repository
                .GetAll<TStore>()
                .ConfigureAwait(false);

            return regions
                .Select(this.fromStore)
                .ToList();
        }
    }
}

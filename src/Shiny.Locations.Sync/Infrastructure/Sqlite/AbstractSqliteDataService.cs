using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Shiny.Locations.Sync.Infrastructure.Sqlite
{
    public abstract class AbstractSqliteDataService<TData, TDomain>
            where TDomain : LocationEvent
            where TData : new()
    {
        readonly SyncSqliteConnection conn;
        protected AbstractSqliteDataService(SyncSqliteConnection conn) => this.conn = conn;


        protected abstract TDomain ToDomain(TData data);
        protected abstract TData FromDomain(TDomain domain);
        public virtual Task<int> GetPendingCount() => this.conn.Table<TData>().CountAsync();


        public virtual async Task Create(TDomain locationEvent)
        {
            var data = this.FromDomain(locationEvent);
            await this.conn.InsertAsync(data);
        }


        public virtual async Task<List<TDomain>> GetAll()
        {
            var data = await this.conn.Table<TData>().ToListAsync();
            return data.Select(this.ToDomain).ToList();
        }


        public virtual async Task Remove(TDomain domain)
        {
            var data = this.FromDomain(domain);
            await this.conn.DeleteAsync(data);
        }
    }
}

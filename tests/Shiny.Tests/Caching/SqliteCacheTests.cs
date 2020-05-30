using System;
using System.IO;
using System.Threading.Tasks;

using Shiny.Caching;
using Shiny.Infrastructure;
using Shiny.Integrations.Sqlite;


namespace Shiny.Tests.Caching
{
    public class SqliteCacheTests : BaseCacheTests, IAsyncDisposable
    {
        readonly ShinySqliteConnection conn;
        public SqliteCacheTests() => this.conn = Helper.GetConnection();


        public async ValueTask DisposeAsync()
        {
            await this.conn.CloseAsync();
            File.Delete(this.conn.DatabasePath);
        }


        protected override ICache Create()
            => new SqliteCache(this.conn, new ShinySerializer());
    }
}

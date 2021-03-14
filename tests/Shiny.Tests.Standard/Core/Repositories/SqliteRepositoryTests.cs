using System;
using System.IO;
using Shiny.Infrastructure;
using Shiny.Integrations.Sqlite;


namespace Shiny.Tests.Core.Repositories
{
    public class SqliteRepositoryTests : BaseRepositoryTests, IDisposable
    {
        readonly ShinySqliteConnection conn;
        public SqliteRepositoryTests() => this.conn = Helper.GetConnection();


        protected override IRepository Create()
            => new SqliteRepository(this.conn, new ShinySerializer());


        public void Dispose() => this.conn?.Purge();
    }
}

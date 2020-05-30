using System;
using Shiny.Infrastructure;
using Shiny.Integrations.Sqlite;


namespace Shiny.Tests.Repositories
{
    public class SqliteRepositoryTests : BaseRepositoryTests
    {
        readonly ShinySqliteConnection conn;
        public SqliteRepositoryTests() => this.conn = Helper.GetConnection();


        protected override IRepository Create()
            => new SqliteRepository(conn, new ShinySerializer());
    }
}

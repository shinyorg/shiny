using System;
using Shiny.Integrations.Sqlite;


namespace Shiny.Tests
{
    public static class Helper
    {
        public static ShinySqliteConnection GetConnection()
            => new ShinySqliteConnection(new FileSystemImpl());
    }
}

using System;
using Microsoft.Extensions.Logging;


namespace Shiny.Integrations.Sqlite
{
    public class SqliteLoggerProvider : ILoggerProvider
    {
        readonly ShinySqliteConnection conn;
        //IOptionsMonitor<> bla
        public SqliteLoggerProvider(ShinySqliteConnection conn)
        {
            this.conn = conn;
        }


        public ILogger CreateLogger(string categoryName) => new SqliteLogger(this.conn);
        public void Dispose() { }
    }
}

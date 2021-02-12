using System;
using Microsoft.Extensions.Logging;


namespace Shiny.Integrations.Sqlite
{
    public class SqliteLoggerProvider : ILoggerProvider
    {
        //IOptionsMonitor<> bla
        public SqliteLoggerProvider(ShinySqliteConnection conn)
        {

        }


        public ILogger CreateLogger(string categoryName) => throw new NotImplementedException();
        public void Dispose() => throw new NotImplementedException();
    }
}

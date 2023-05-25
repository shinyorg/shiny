using Microsoft.Extensions.Logging;

namespace Shiny.Logging.Sqlite;


public class SqliteLoggerProvider : ILoggerProvider
{
    readonly LogLevel logLevel;
    readonly LoggingSqliteConnection conn;

    public SqliteLoggerProvider(LogLevel logLevel, LoggingSqliteConnection conn)
    {
        this.logLevel = logLevel;
        this.conn = conn;
    }

    public ILogger CreateLogger(string categoryName) => new SqliteLogger(this.logLevel, this.conn);
    public void Dispose() { }
}
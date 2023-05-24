using Microsoft.Extensions.Logging;

namespace Shiny.Logging.Sqlite;


public class SqliteLoggerProvider : ILoggerProvider
{
    readonly LogLevel logLevel;
    public SqliteLoggerProvider(LogLevel logLevel) => this.logLevel = logLevel;

    public ILogger CreateLogger(string categoryName) => new SqliteLogger(this.logLevel);
    public void Dispose() { }
}
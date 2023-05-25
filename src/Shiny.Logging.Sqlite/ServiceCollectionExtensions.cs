using Microsoft.Extensions.Logging;
using Shiny.Logging.Sqlite;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add SQLite logging
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="dbPath">The path used to store the sqlite database</param>
    /// <param name="logLevel">The minimum log level to use</param>
    public static void AddSqlite(this ILoggingBuilder builder, string dbPath = ".", LogLevel logLevel = LogLevel.Trace)
    {
        var conn = LoggingSqliteConnection.CreateInstance(dbPath);
        builder.AddProvider(new SqliteLoggerProvider(logLevel, conn));
    }
}

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Shiny.Logging.Sqlite;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add SQLite logging
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="logLevel"></param>
    public static void AddSqlite(this ILoggingBuilder builder, LogLevel logLevel = LogLevel.Warning)
    {
        //builder.Services.TryAddSingleton<ShinySqliteConnection>();
        builder.AddProvider(new SqliteLoggerProvider(logLevel));
    }
}

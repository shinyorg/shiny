using Shiny.Hosting;

namespace Sample.Infrastructure;


public class SqliteLoggerProvider : ILoggerProvider
{
    readonly LogLevel logLevel;
    public SqliteLoggerProvider(LogLevel logLevel) => this.logLevel = logLevel;

    public ILogger CreateLogger(string categoryName) => new SqliteLogger(this.logLevel, Host.Current.Services.GetRequiredService<SampleSqliteConnection>());
    public void Dispose() { }
}
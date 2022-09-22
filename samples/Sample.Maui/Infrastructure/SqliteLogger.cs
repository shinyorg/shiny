using System.Reactive.Disposables;

namespace Sample.Infrastructure;


public class SqliteLogger : ILogger
{
    readonly LogLevel configLogLevel;
    readonly SampleSqliteConnection conn;


    public SqliteLogger(LogLevel logLevel, SampleSqliteConnection conn)
    {
        this.configLogLevel = logLevel;
        this.conn = conn;
    }


    public IDisposable BeginScope<TState>(TState state) => Disposable.Empty;
    public bool IsEnabled(LogLevel logLevel) => logLevel >= this.configLogLevel;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!this.IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        this.conn.GetConnection().Insert(new Log
        {
            Category = "ERROR",
            Text = message,
            Detail = exception?.ToString() ?? "No exception detail",
            //Parameters <= could come from scope
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}
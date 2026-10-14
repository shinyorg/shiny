using Microsoft.Extensions.Logging;

namespace Sample.CallDirectory;


/// <summary>Writes to stdout (visible with simctl launch --console) and to the on-screen log.</summary>
public static class AppLog
{
    public static event Action<string>? Written;

    public static void Write(string line)
    {
        Console.WriteLine("SHINYCD " + line);
        Written?.Invoke(line);
    }
}


public sealed class AppLogProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new Logger(categoryName);
    public void Dispose() { }

    sealed class Logger(string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => AppLog.Write($"[{logLevel}] {category.Split('.').Last()}: {formatter(state, exception)}{(exception == null ? "" : " " + exception)}");
    }
}

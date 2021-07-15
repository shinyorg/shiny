using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;


namespace Shiny.Logging
{
    public class DebugLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel)
            => Debugger.IsAttached && logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message))
                return;

            message = $"{logLevel}: {message}";
            if (exception != null)
                message += $"{Environment.NewLine}{Environment.NewLine} {exception}";

            Debug.WriteLine(message);
        }
    }
}

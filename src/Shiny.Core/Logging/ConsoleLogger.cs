using System;
using Microsoft.Extensions.Logging;


namespace Shiny.Logging
{
    public class ConsoleLogger : ILogger
    {
        readonly string categoryName;
        readonly LogLevel level;


        public ConsoleLogger(string categoryName, LogLevel logLevel)
        {
            this.categoryName = categoryName;
            this.level = logLevel;
        }

        public IDisposable BeginScope<TState>(TState state)
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel)
            => this.level >= logLevel;

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

            Console.WriteLine(message);
        }
    }
}

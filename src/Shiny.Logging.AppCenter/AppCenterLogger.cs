using System;
using System.Collections.Generic;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Logging;


namespace Shiny.Logging.AppCenter
{
    public class AppCenterLogger : ILogger
    {
        readonly string categoryName;
        readonly LogLevel configLogLevel;


        public AppCenterLogger(string categoryName, LogLevel logLevel)
        {
            this.categoryName = categoryName;
            this.configLogLevel = logLevel;
        }


        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= this.configLogLevel;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!this.IsEnabled(logLevel))
                return;

            // TODO: I need scopes
            var message = formatter(state, exception);
            if (logLevel >= LogLevel.Error)
            {
                exception ??= new Exception(message);

                Crashes.TrackError(
                    exception,
                    new Dictionary<string, string>
                    {
                        { "Message", message }
                    }
                );
            }
            else
            {
                Analytics.TrackEvent($"[{logLevel} - {this.categoryName}] {message}");
            }
        }
    }
}

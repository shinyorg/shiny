using System;
using System.Reactive.Disposables;
using Microsoft.Extensions.Logging;


namespace Shiny.Logging
{
    public class ShinyLogger : ILogger
    {
        readonly ILogger[] loggers;
        public ShinyLogger(ILogger[] loggers) => this.loggers = loggers;


        public IDisposable BeginScope<TState>(TState state)
        {
            var composite = new CompositeDisposable();
            foreach (var logger in this.loggers)
            {
                var dispose = logger.BeginScope(state);
                if (dispose != null)
                    composite.Add(dispose);
            }
            return composite;
        }


        public bool IsEnabled(LogLevel logLevel)
        {
            foreach (var logger in this.loggers)
            {
                if (logger.IsEnabled(logLevel))
                    return true;
            }
            return false;
        }


        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            foreach (var logger in this.loggers)
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, state, exception, formatter);
                }
            }
        }
    }
}

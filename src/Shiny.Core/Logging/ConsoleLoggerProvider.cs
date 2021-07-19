using System;
using Microsoft.Extensions.Logging;


namespace Shiny.Logging
{
    public class ConsoleLoggerProvider : ILoggerProvider
    {
        readonly LogLevel logLevel;
        public ConsoleLoggerProvider(LogLevel logLevel)
            => this.logLevel = logLevel;

        public ILogger CreateLogger(string categoryName) => new ConsoleLogger(categoryName, this.logLevel);
        public void Dispose() { }
    }
}

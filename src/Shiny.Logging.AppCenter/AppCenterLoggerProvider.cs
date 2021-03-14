using System;
using Microsoft.Extensions.Logging;


namespace Shiny.Logging.AppCenter
{
    public class AppCenterLoggerProvider : ILoggerProvider
    {
        readonly LogLevel logLevel;
        public AppCenterLoggerProvider(LogLevel logLevel) => this.logLevel = logLevel;


        public ILogger CreateLogger(string categoryName) => new AppCenterLogger(categoryName, this.logLevel);
        public void Dispose() { }
    }
}

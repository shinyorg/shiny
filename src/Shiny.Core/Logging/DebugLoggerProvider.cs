using System;
using Microsoft.Extensions.Logging;


namespace Shiny.Logging
{
    public class DebugLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new DebugLogger(categoryName);
        public void Dispose() { }
    }
}

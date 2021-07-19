using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;


namespace Shiny.Logging
{
    public class ShinyLoggerFactory : ILoggerFactory
    {
        readonly IList<ILoggerProvider> providers;


        public ShinyLoggerFactory(IEnumerable<ILoggerProvider> providers)
            => this.providers = providers.ToList();


        public void AddProvider(ILoggerProvider provider) { }


        public ILogger CreateLogger(string categoryName)
        {
            var loggers = new List<ILogger>();
            foreach (var provider in this.providers)
            {
                var logger = provider.CreateLogger(categoryName);
                loggers.Add(logger);
            }

#if DEBUG
            loggers.Add(new DebugLogger());
#else
            if (loggers.Count == 0)
                loggers.Add(new DebugLogger());
#endif

            return new ShinyLogger(loggers.ToArray());
        }


        public void Dispose()
        {
        }
    }
}
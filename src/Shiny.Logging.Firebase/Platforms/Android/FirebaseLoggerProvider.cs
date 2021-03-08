using System;
using Firebase.Analytics;
using Microsoft.Extensions.Logging;


namespace Shiny.Logging.Firebase
{
    public class FirebaseLoggerProvider : ILoggerProvider
    {
        readonly LogLevel logLevel;
        public FirebaseLoggerProvider(LogLevel logLevel) => this.logLevel = logLevel;


        public ILogger CreateLogger(string categoryName)
        {
            var androidContext = ShinyHost.Resolve<IAndroidContext>();
            var instance = FirebaseAnalytics.GetInstance(androidContext.AppContext);
            return new FirebaseLogger(categoryName, this.logLevel, instance);
        }

        public void Dispose() { }
    }
}

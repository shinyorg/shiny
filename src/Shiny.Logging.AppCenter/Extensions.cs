using System;
using System.Collections.Generic;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Logging;
using Shiny.Logging.AppCenter;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;


namespace Shiny
{
    public static class Extensions
    {
        public static void AddAppCenter(this ILoggingBuilder builder,
                                        string appCenterSecret,
                                        LogLevel logLevel = LogLevel.Warning)
        {
            if (String.IsNullOrWhiteSpace(appCenterSecret) || logLevel == LogLevel.Warning)
                return;

            builder.AddProvider(new AppCenterLoggerProvider(logLevel));
            var list = new List<Type>(2);
            list.Add(typeof(Crashes));

            if (logLevel <= LogLevel.Information)
                list.Add(typeof(Analytics));

            AppCenter.Start(appCenterSecret, list.ToArray());
        }
    }
}

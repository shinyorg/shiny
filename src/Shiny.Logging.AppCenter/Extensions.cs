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
            builder.AddProvider(new AppCenterLoggerProvider(logLevel));
            if (!String.IsNullOrWhiteSpace(appCenterSecret))
            {
                AppCenter.Start(
                    appCenterSecret,
                    typeof(Crashes),
                    typeof(Analytics)
                );
            }
        }
    }
}

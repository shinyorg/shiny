using System;
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
        /// <summary>
        /// Adds AppCenter to the logging provider
        /// </summary>
        /// <param name="builder">The logging builder</param>
        /// <param name="appCenterSecret">Your appcenter secret key for any/all platforms you use.  If you don't set this value, it is assumed you will initialize AppCenter externally</param>
        /// <param name="logLevel">The minimum loglevel you wish to use - defaults to warning</param>
        public static void AddAppCenter(this ILoggingBuilder builder,
                                        string? appCenterSecret = null,
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

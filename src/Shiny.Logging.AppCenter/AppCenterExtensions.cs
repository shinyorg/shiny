#if PLATFORM
using System;
using System.Collections.Generic;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Logging;
using Shiny.Logging.AppCenter;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Shiny;


public static class AppCenterExtensions
{
    /// <summary>
    /// Adds AppCenter to the logging provider
    /// </summary>
    /// <param name="builder">The logging builder</param>
    /// <param name="appCenterSecret">Your appcenter secret key for any/all platforms you use.  If you don't set this value, it is assumed you will initialize AppCenter externally</param>
    /// <param name="minimumLogLevel">The minimum loglevel you wish to use - defaults to warning</param>
    /// <param name="enableVerboseInternalLogging">Enable internal verbose logging with AppCenter</param>
    /// <param name="additionalAppCenterPackages">Additional appcenter types to initialize</param>
    public static void AddAppCenter(
        this ILoggingBuilder builder,
        string? appCenterSecret = null,
        LogLevel minimumLogLevel = LogLevel.Warning,
        bool enableVerboseInternalLogging = false,
        params Type[] additionalAppCenterPackages
    )
    {
        builder.AddProvider(new AppCenterLoggerProvider(minimumLogLevel));
        if (!String.IsNullOrWhiteSpace(appCenterSecret))
        {
            var list = new List<Type> { typeof(Crashes), typeof(Analytics) };
            if (additionalAppCenterPackages.Length > 0)
                list.AddRange(additionalAppCenterPackages);

            if (!AppCenter.Configured)
            {
                if (enableVerboseInternalLogging)
                    AppCenter.LogLevel = Microsoft.AppCenter.LogLevel.Verbose;

                AppCenter.Start(appCenterSecret, list.ToArray());
            }
        }
    }
}
#endif
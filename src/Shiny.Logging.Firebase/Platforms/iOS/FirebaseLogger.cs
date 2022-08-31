using System;
using System.Collections.Generic;
using Foundation;
using Microsoft.Extensions.Logging;

//https://www.thewissen.io/using-firebase-analytics-in-your-xamarin-forms-app/

namespace Shiny.Logging.Firebase;


public class FirebaseLogger : ILogger
{
    readonly string categoryName;
    readonly LogLevel configLogLevel;


    public FirebaseLogger(string categoryName, LogLevel logLevel)
    {
        this.categoryName = categoryName;
        this.configLogLevel = logLevel;
    }


    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
    public bool IsEnabled(LogLevel logLevel) => logLevel >= this.configLogLevel && global::Firebase.Crashlytics.Crashlytics.SharedInstance.IsCrashlyticsCollectionEnabled;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!this.IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        if (logLevel >= LogLevel.Error)
        {
            var crashes = global::Firebase.Crashlytics.Crashlytics.SharedInstance;

            if (!String.IsNullOrWhiteSpace(message))
                crashes.LogCallerInformation(message);

            var error = new NSError(new NSString(exception.ToString()), 0);
            crashes.RecordError(error);
        }
        else
        {
            global::Firebase.Analytics.Analytics.LogEvent(message, new Dictionary<object, object>());
        }
    }
}

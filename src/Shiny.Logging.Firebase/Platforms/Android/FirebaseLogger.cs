using System;
using Android.Content;
using Android.OS;
using Firebase.Analytics;
using Java.Lang;
using Microsoft.Extensions.Logging;
using Exception = System.Exception;


namespace Shiny.Logging.Firebase
{
    public class FirebaseLogger : ILogger
    {
        readonly string categoryName;
        readonly LogLevel configLogLevel;
        readonly FirebaseAnalytics analysis;


        public FirebaseLogger(
            string categoryName,
            LogLevel logLevel,
            FirebaseAnalytics analyticsInstance
        )
        {
            this.categoryName = categoryName;
            this.configLogLevel = logLevel;
            this.analysis = analyticsInstance;
        }


        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= this.configLogLevel;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!this.IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            if (logLevel >= LogLevel.Error)
            {

                var throwable = new Throwable(exception.ToString());
                //global::Firebase.Crashlytics.FirebaseCrashlytics.Instance.SetCustomKey
                global::Firebase.Crashlytics.FirebaseCrashlytics.Instance.RecordException(throwable);
            }
            else
            {
                var bundle = new Bundle();
                bundle.PutString("Category", this.categoryName);
                this.analysis.LogEvent(message, bundle);
                //this.analysis.SetUserProperty
                //this.analysis.SetCurrentScreen
            }
        }
    }
}

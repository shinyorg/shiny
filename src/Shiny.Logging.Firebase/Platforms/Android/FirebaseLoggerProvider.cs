using System;
using Firebase.Analytics;
using Microsoft.Extensions.Logging;
using Android.App;

namespace Shiny.Logging.Firebase;


public class FirebaseLoggerProvider : ILoggerProvider
{
    readonly LogLevel logLevel;
    public FirebaseLoggerProvider(LogLevel logLevel) => this.logLevel = logLevel;


    public ILogger CreateLogger(string categoryName)
    {
        var instance = FirebaseAnalytics.GetInstance(Application.Context);
        return new FirebaseLogger(categoryName, this.logLevel, instance);
    }

    public void Dispose() { }
}

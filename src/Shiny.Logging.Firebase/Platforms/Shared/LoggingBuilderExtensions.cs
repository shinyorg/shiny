using System;
using Microsoft.Extensions.Logging;

namespace Shiny;


//https://www.thewissen.io/using-firebase-analytics-in-your-xamarin-forms-app/
//https://medium.com/@hakimgulamali88/firebase-crashlytics-with-xamarin-5421089bb561
public static class LoggingBuilderExtensions
{
    public static void AddFirebase(this ILoggingBuilder builder, LogLevel logLevel = LogLevel.Warning)
    {
        builder.AddProvider(new Shiny.Logging.Firebase.FirebaseLoggerProvider(logLevel));

#if IOS
        // ensure GoogleService-Info.plist
        Firebase.Core.App.Configure();
#elif ANDROID
        // string resource com.crashlytics.android.build_id
#endif
    }
}

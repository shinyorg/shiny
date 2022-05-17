using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace Shiny.Hosting;


public static class ShinyExtensions
{
    public static MauiAppBuilder UseShiny(this MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android
                //.OnApplicationCreate((app) =>
                //{

                //})
                .OnRequestPermissionsResult((activity, requestCode, permissions, grantResults) => Host.Current.Lifecycle().OnRequestPermissionsResult(requestCode, permissions, grantResults))
                .OnActivityResult((activity, requestCode, result, intent) => Host.Current.Lifecycle().OnActivityResult(requestCode, result, intent))
                .OnNewIntent((activity, intent) => Host.Current.Lifecycle().OnNewIntent(intent))
            );
#elif IOS
            // TODO: missing push events & handle background url for http transfers
            events.AddiOS(ios => ios
                .FinishedLaunching((app, options) => Host.Current.Lifecycle().FinishedLaunching(options))
                .ContinueUserActivity((app, activity, handler) => Host.Current.Lifecycle().OnContinueUserActivity(activity, handler))
                //.WillEnterForeground(app =>
                //{

                //})
                //.DidEnterBackground(app =>
                //{

                //})
            );
#endif
        });

        return builder;
    }

    public static void RunShiny(this MauiApp app)
    {
        var host = new Host(app.Services, app.Services.GetRequiredService<ILoggerFactory>());
        host.Run();
        // TODO: run the build for shiny hooks, is it too late?
    }
}

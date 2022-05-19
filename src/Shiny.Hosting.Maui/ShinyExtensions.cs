using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Shiny.Hosting;

namespace Shiny;


public static class ShinyExtensions
{
    public static MauiAppBuilder UseShiny(this MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android
                // Shiny will supply app foreground/background events
                .OnRequestPermissionsResult((activity, requestCode, permissions, grantResults) => Host.Current.Lifecycle().OnRequestPermissionsResult(activity, requestCode, permissions, grantResults))
                .OnActivityResult((activity, requestCode, result, intent) => Host.Current.Lifecycle().OnActivityResult(activity, requestCode, result, intent))
                .OnNewIntent((activity, intent) => Host.Current.Lifecycle().OnNewIntent(activity, intent))
            );
#elif IOS
            // Shiny will supply push events & handle background url for http transfers
            events.AddiOS(ios => ios
                .FinishedLaunching((_, options) => Host.Current.Lifecycle().FinishedLaunching(options))
                .ContinueUserActivity((_, activity, handler) => Host.Current.Lifecycle().OnContinueUserActivity(activity, handler))
                .WillEnterForeground(_ => Host.Current.Lifecycle().OnAppForegrounding())
                .DidEnterBackground(_ => Host.Current.Lifecycle().OnAppBackgrounding())
            );
#endif
        });

        return builder;
    }

    public static void RunShiny(this MauiApp app)
    {
        // TODO: ensure registrations?
        var host = new Host(app.Services, app.Services.GetRequiredService<ILoggerFactory>());
        host.Run();
        // TODO: run the build for shiny hooks, is it too late?
    }
}

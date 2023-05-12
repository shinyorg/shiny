using Microsoft.Maui.LifecycleEvents;
using Shiny.Hosting;
using Shiny.Infrastructure;

namespace Shiny;


public static class ShinyExtensions
{
    public static MauiAppBuilder UseShiny(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<IMauiInitializeService, ShinyMauiInitializationService>();
        builder.Services.AddShinyCoreServices();

        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android
                // Shiny will supply app foreground/background events
                .OnRequestPermissionsResult((activity, requestCode, permissions, grantResults) => Host.Lifecycle.OnRequestPermissionsResult(activity, requestCode, permissions, grantResults))
                .OnActivityResult((activity, requestCode, result, intent) => Host.Lifecycle.OnActivityResult(activity, requestCode, result, intent))
                .OnNewIntent((activity, intent) => Host.Lifecycle.OnNewIntent(activity, intent))
            );
#elif IOS || MACCATALYST
            // Shiny will supply push events & handle background url for http transfers
            events.AddiOS(ios => ios
                .FinishedLaunching((_, options) => Host.Lifecycle.FinishedLaunching(options))
                .ContinueUserActivity((_, activity, handler) => Host.Lifecycle.OnContinueUserActivity(activity, handler))
                .WillEnterForeground(_ => Host.Lifecycle.OnAppForegrounding())
                .DidEnterBackground(_ => Host.Lifecycle.OnAppBackgrounding())
            );
#endif
        });

        return builder;
    }
}

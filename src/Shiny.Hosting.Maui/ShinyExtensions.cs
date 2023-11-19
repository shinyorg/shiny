using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
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
                .OnCreate((activity, savedInstanceState) => Host.Lifecycle.OnActivityOnCreate(activity, savedInstanceState))
                .OnRequestPermissionsResult((activity, requestCode, permissions, grantResults) => Host.Lifecycle.OnRequestPermissionsResult(activity, requestCode, permissions, grantResults))
                .OnActivityResult((activity, requestCode, result, intent) => Host.Lifecycle.OnActivityResult(activity, requestCode, result, intent))
                .OnNewIntent((activity, intent) => Host.Lifecycle.OnNewIntent(activity, intent))
            );
#elif APPLE
            // Shiny will supply push events & handle background url for http transfers
            events.AddiOS(ios => ios
                .ContinueUserActivity((_, activity, handler) => Host.Lifecycle.OnContinueUserActivity(activity, handler))
            );
#elif WINDOWS
            events.AddWindows(win => win
                .OnLaunching((app, args) => { })
                .OnClosed((app, args) => { })
                .OnVisibilityChanged((app, args) => { })
            );
#endif
        });

        return builder;
    }
}

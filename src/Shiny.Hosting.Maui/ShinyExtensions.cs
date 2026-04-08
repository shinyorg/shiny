#if PLATFORM
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

#if WINDOWS
        // Capture the WinUI 3 UI dispatcher so Shiny.Core can marshal work to the
        // main thread without taking a WinUI/WindowsAppSDK dependency at the Core layer.
        // UseShiny() is invoked from MauiProgram.CreateMauiApp, which runs inside
        // MauiWinUIApplication.OnLaunched on the UI thread - so GetForCurrentThread()
        // returns the correct dispatcher.
        var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        if (dispatcherQueue != null)
        {
            WindowsPlatform.MainThreadHandler = action =>
            {
                if (dispatcherQueue.HasThreadAccess)
                    action();
                else
                    dispatcherQueue.TryEnqueue(() => action());
            };
        }
#endif

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
#endif

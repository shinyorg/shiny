using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;
using Xunit.Runners.Maui;

namespace Shiny.Tests;


public static class MauiProgram
{
    public static IConfiguration Configuration { get; private set; } = null!;


    public static MauiApp CreateMauiApp()
    {
        System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

        Configuration = new ConfigurationBuilder()
            .AddJsonPlatformBundle(optional: false)
            .Build();

        var builder = MauiApp.CreateBuilder();

        builder
            .ConfigureTests(new TestOptions
            {
                Assemblies =
                {
                    typeof(MauiProgram).Assembly
                }
            })
            .UseShiny() // this is somewhat of a hack as it hooks the shiny events BUT to the current host provider
            .UseVisualRunner()
            .ConfigureLifecycleEvents(lc =>
            {
#if ANDROID
                lc.AddAndroid(x => x
                    .OnCreate((_, _) => DeviceDisplay.KeepScreenOn = true)
                );
#else
                DeviceDisplay.KeepScreenOn = true;
#endif
            });

        //builder.Logging.AddDebug();

        return builder.Build();
    }
}


//builder.Services.AddSingleton<IMauiInitializeService, ShinyMauiInitializationService>();
//        builder.Services.AddShinyCoreServices();

//        builder.ConfigureLifecycleEvents(events =>
//        {
//#if ANDROID
//            events.AddAndroid(android => android
//                // Shiny will supply app foreground/background events
//                .OnRequestPermissionsResult((activity, requestCode, permissions, grantResults) => Host.Lifecycle.OnRequestPermissionsResult(activity, requestCode, permissions, grantResults))
//                .OnActivityResult((activity, requestCode, result, intent) => Host.Lifecycle.OnActivityResult(activity, requestCode, result, intent))
//                .OnNewIntent((activity, intent) => Host.Lifecycle.OnNewIntent(activity, intent))
//            );
//#elif APPLE
//            // Shiny will supply push events & handle background url for http transfers
//            events.AddiOS(ios => ios
//                .FinishedLaunching((_, options) => Host.Lifecycle.FinishedLaunching(options))
//                .ContinueUserActivity((_, activity, handler) => Host.Lifecycle.OnContinueUserActivity(activity, handler))
//                .WillEnterForeground(_ => Host.Lifecycle.OnAppForegrounding())
//                .DidEnterBackground(_ => Host.Lifecycle.OnAppBackgrounding())
//            );
//#elif WINDOWS
//            events.AddWindows(win => win
//                .OnLaunching((app, args) => { })
//                .OnClosed((app, args) => { })
//                .OnVisibilityChanged((app, args) => { })
//            );
//#endif
//        });
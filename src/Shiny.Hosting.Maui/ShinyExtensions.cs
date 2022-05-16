using Microsoft.Maui.LifecycleEvents;

namespace Shiny.Hosting;


public static class ShinyExtensions
{
    public static MauiAppBuilder UseShiny(this MauiAppBuilder builder)
    {
        // TODO: capture services here for host run
        // TODO: shinyhostbuilder?
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android
                //.OnApplicationCreate((app) =>
                //{

                //})
                //.OnResume((activity, intent) =>
                //{

                //})
                .OnPause(activity =>
                {
                })
                .OnRequestPermissionsResult((activity, requestCode, permissions, grantResults) =>
                {

                })
                .OnActivityResult((activity, requestCode, result, intent) =>
                {

                })
            );
#elif IOS
            // TODO: missing push events & handle background url for http transfers
            events.AddiOS(ios => ios
                .FinishedLaunching((app, options) =>
                {
                    return true; //??
                })
                .ContinueUserActivity((app, activity, handler) =>
                {
                    return true; // NO
                })
                .WillEnterForeground(app =>
                {

                })
                .DidEnterBackground(app =>
                {

                })
            );
#endif
        });

        return builder;
    }

    public static void RunShiny(this MauiApp app)
    {
        // TODO: run startup tasks
        // TODO: run the build for shiny hooks, is it too late?
        // TODO: set static shinyhost value
    }
}

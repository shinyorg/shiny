//namespace Shiny;

//using Microsoft.Extensions.Logging;
//using Microsoft.Maui.Hosting;
//using Microsoft.Maui.LifecycleEvents;

//public static class BridgeExtensions
//{
//    public static MauiAppBuilder UseShiny(this MauiAppBuilder builder)
//    {
//        // capture this if needed: builder.Services;
//        // TODO: replace MAUI configuration

//        // For shiny to "work", I need a reference to the ServiceProvider, if I use a lifecycle event in MAUI, is it too late?  Will the broadcast receivers fired already?
//        // could also force a "post build" step like
//            // var app = builder.Build(); app.StartShiny(); to create hooks

//        builder.ConfigureLifecycleEvents(x =>
//        {
//#if IOS
//#elif ANDROID
//            //MauiApplication.Current.Services // static
//#endif
//        });
//        return builder;
//    }


//    /// <summary>
//    /// Initializes Shiny to be used within MAUI context
//    /// </summary>
//    /// <param name="app"></param>
//    public static void StartShiny(this MauiApp app)
//    {
//        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
//        ShinyHost.Init(app.Services, loggerFactory);
//    }
//}

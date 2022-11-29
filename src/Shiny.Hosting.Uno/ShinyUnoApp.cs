//using System;
//using System.Diagnostics;
//using Microsoft.UI.Xaml;
//using Shiny.Hosting;
//#if IOS
//using Foundation;
//using ObjCRuntime;
//using UIKit;
//#endif
//using static Uno.CompositionConfiguration;


//namespace Shiny;


//public abstract class ShinyUnoApp : Application
//{
//    protected abstract IHost CreateShinyApp();

//    protected override void OnLaunched(LaunchActivatedEventArgs args)
//    {
//        // TODO: hostbuilder does not exist in standard .NET at the moment
//        this.CreateShinyApp();
//        base.OnLaunched(args);
//    }

//#if APPLE
//    public override void WillEnterForeground(UIApplication application)
//        => Host.Current.Lifecycle().OnAppForegrounding();

//    public override void DidEnterBackground(UIApplication application)
//        => Host.Current.Lifecycle().OnAppBackgrounding();

//    public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
//        => Host.Current.Lifecycle().OnContinueUserActivity(userActivity, completionHandler);

//    public override bool WillFinishLaunching(UIApplication application, NSDictionary launchOptions)
//        => Host.Current.Lifecycle().FinishedLaunching(launchOptions);

//    public override void HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler)
//        => Host.Current.Lifecycle().OnHandleEventsForBackgroundUrl(sessionIdentifier, completionHandler);

//    //[Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
//    //public void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
//    //{
//    //    throw new System.NotImplementedException();
//    //}

//    //public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
//    //{
//    //    base.DidReceiveRemoteNotification(application, userInfo, completionHandler);
//    //}
//#elif ANDROID

//    // TODO: I need lifecycle against mainactivity
//     // Shiny will supply app foreground/background events
//                //.OnRequestPermissionsResult((activity, requestCode, permissions, grantResults) => Host.Current.Lifecycle().OnRequestPermissionsResult(activity, requestCode, permissions, grantResults))
//                //.OnActivityResult((activity, requestCode, result, intent) => Host.Current.Lifecycle().OnActivityResult(activity, requestCode, result, intent))
//                //.OnNewIntent((activity, intent) => Host.Current.Lifecycle().OnNewIntent(activity, intent))
//#endif
//}
using System;
using Foundation;
using Shiny.Jobs;
using UIKit;
using Xamarin.Forms.Platform.iOS;


namespace Shiny
{
    public class ShinyFormsAppDelegate<T> : FormsApplicationDelegate where T : IShinyStartup, new()
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            iOSShinyHost.Init(new T());
            return base.FinishedLaunching(app, options);
        }


        public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler)
            => JobManager.OnBackgroundFetch(completionHandler);


        //public override void HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler)
        //    => HttpTransferManager.SetCompletionHandler(sessionIdentifier, completionHandler);
    }
}

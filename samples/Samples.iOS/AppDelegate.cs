using System.Reactive.Linq;
using Foundation;
using Shiny;
using Shiny.Notifications;
using UIKit;
using Samples.Infrastructure;
using UserNotifications;


[assembly: Shiny.ShinyApplication(
    ShinyStartupTypeName = "Samples.SampleStartup",
    XamarinFormsAppTypeName = "Samples.App"
)]
namespace Samples.iOS
{
    [Foundation.Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        partial void OnPostFinishedLaunching(UIApplication app, NSDictionary options)
        {
            // for the people who don't know how iOS notifications work in the foreground!
            var dialogs = ShinyHost.Resolve<IDialogs>();
            ShinyHost
                .Resolve<iOSNotificationDelegate>()
                .WhenPresented()
                .Where(_ => app.ApplicationState == UIApplicationState.Active)
                .SubOnMainThread(async x =>
                {
                    await dialogs.Snackbar(x.Notification.Request.Content.Body);
                    x.CompletionHandler.Invoke(UNNotificationPresentationOptions.Alert);
                });
        }
    }
}
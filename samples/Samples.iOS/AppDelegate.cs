using Foundation;
using Shiny;
using UIKit;
using Samples.Infrastructure;

[assembly: ShinyApplication(
    ShinyStartupTypeName = "Samples.SampleStartup",
    XamarinFormsAppTypeName = "Samples.App"
)]
namespace Samples.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        partial void OnPostFinishedLaunching(UIApplication app, NSDictionary options)
        {
            // for the people who don't know how iOS notifications work in the foreground!
            ShinyHost.Resolve<AppleLifecycle>().RegisterForNotificationPresentation(async notification =>
            {
                if (app.ApplicationState == UIApplicationState.Active)
                    await ShinyHost.Resolve<IDialogs>().Snackbar(notification.Request?.Content?.Body ?? "NO CONTENT");
            });
        }
    }
}
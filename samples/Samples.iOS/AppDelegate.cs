using System;
using Foundation;
using Xamarin.Forms.Platform.iOS;
using Shiny;

[assembly: ShinyApplication(
    ShinyStartupTypeName = "Samples.SampleStartup",
    XamarinFormsAppTypeName = "Samples.App"
)]
[assembly: ShinyGeneratorDebug]


namespace Samples.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : FormsApplicationDelegate
    {
        //partial void OnPreFinishedLaunching(UIApplication app, NSDictionary options) => Forms.SetFlags(
        //    "SwipeView_Experimental",
        //    "Expander_Experimental",
        //    "RadioButton_Experimental"
        //);

        //public override bool ContinueUserActivity(UIKit.UIApplication application, NSUserActivity userActivity, UIKit.UIApplicationRestorationHandler completionHandler)
        //{
        //    //completionHandler()
        //    return true;
        //}
    }
}
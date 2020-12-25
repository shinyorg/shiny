using System;
using Foundation;
using UIKit;
using Xamarin.Forms;
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
        //void Init(UIApplication app, NSDictionary options) => Forms.SetFlags(
        //    "SwipeView_Experimental",
        //    "Expander_Experimental",
        //    "RadioButton_Experimental"
        //);
    }
}
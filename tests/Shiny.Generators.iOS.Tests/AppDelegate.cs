using System;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;


namespace Shiny.Generators.iOS.Tests
{
    [Register("AppDelegate")]
    public partial class AppDelegate : FormsApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            // this needs to be loaded before EVERYTHING
            //this.ShinyFinishedLaunching(new SampleStartup());
            Forms.SetFlags(
                "SwipeView_Experimental",
                "Expander_Experimental"
            );
            Forms.Init();
            //this.LoadApplication(new App());
            return base.FinishedLaunching(app, options);
        }
    }
}

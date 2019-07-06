using System;
using Foundation;
using UIKit;
using Xamarin.Forms.Platform.iOS;


namespace Shiny
{
    public class ShinyFormsAppDelegate : FormsApplicationDelegate
    {
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            //iOSShinyHost.Init(new SampleStartup());
            return base.FinishedLaunching(app, options);
        }
    }
}

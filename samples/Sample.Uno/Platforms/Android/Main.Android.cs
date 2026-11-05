using Android.Runtime;
using Shiny;

namespace Sample.Uno.Droid;


[global::Android.App.Application(
    Label = "Shiny Uno",
    LargeHeap = true,
    HardwareAccelerated = true,
    Theme = "@style/AppTheme"
)]
public class Application : Microsoft.UI.Xaml.NativeApplication
{
    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(() => new App(), javaReference, transfer)
    {
    }


    public override void OnCreate()
    {
        base.OnCreate();

        // Uno calls OnLaunched only once an activity starts (and with the Skia renderer creates the App only then).
        // Building here means Shiny is up when Android starts the process in the background - a job, a push, a
        // geofence - with no activity at all. OnLaunched's GetShinyBuilder gets this same builder and host back.
        this.StartShiny<App>();
    }
}

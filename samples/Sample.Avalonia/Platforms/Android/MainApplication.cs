using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Shiny;

namespace Sample.Avalonia;


[Application]
public class MainApplication : AvaloniaAndroidApplication<App>
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership) { }


    // Avalonia runs this from Application.OnCreate, so Shiny is up even when Android starts the process in the
    // background (a job, a push, a geofence) and no activity is ever created
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder).UseShiny(ShinyStartup.Configure);
}

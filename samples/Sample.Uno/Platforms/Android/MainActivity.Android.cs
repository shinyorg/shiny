using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Shiny.Hosting;

namespace Sample.Uno.Droid;


// Forwards the activity events Shiny needs. Without OnRequestPermissionsResult every Shiny permission request waits
// forever; without OnCreate / OnNewIntent notification taps never reach their delegate. SingleTop so tapping a
// notification while the app is open arrives through OnNewIntent instead of starting a second activity.
[Activity(
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = global::Android.Views.SoftInput.AdjustNothing | global::Android.Views.SoftInput.StateHidden
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
    protected override void OnCreate(Bundle? bundle)
    {
        base.OnCreate(bundle);
        Host.Lifecycle.OnActivityOnCreate(this, bundle);
    }


    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Host.Lifecycle.OnNewIntent(this, intent);
    }


    protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        Host.Lifecycle.OnActivityResult(this, requestCode, resultCode, data);
    }


    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        Host.Lifecycle.OnRequestPermissionsResult(this, requestCode, permissions, grantResults);
    }
}

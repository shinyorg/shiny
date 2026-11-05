using Android.App;
using Android.Content.PM;
using Shiny;

namespace Sample.Avalonia;


// ShinyAvaloniaMainActivity forwards OnCreate / OnNewIntent / OnActivityResult / OnRequestPermissionsResult to Shiny.
// SingleTop so tapping a notification while the app is open arrives through OnNewIntent instead of a second activity.
[Activity(
    Label = "Shiny Avalonia",
    Theme = "@style/Theme.AppCompat.DayNight.NoActionBar",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode
)]
public class MainActivity : ShinyAvaloniaMainActivity
{
}

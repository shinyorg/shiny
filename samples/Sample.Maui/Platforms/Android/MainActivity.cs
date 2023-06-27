using Android.App;
using Android.Content.PM;

namespace Sample.Maui;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges =
        ConfigChanges.ScreenSize |
        ConfigChanges.Orientation |
        ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize |
        ConfigChanges.Density
)]
[IntentFilter(new[] {
    Shiny.ShinyNotificationIntents.NotificationClickAction
})]
public class MainActivity : MauiAppCompatActivity
{
}
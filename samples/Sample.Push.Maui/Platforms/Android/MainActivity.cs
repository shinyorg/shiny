using Android.App;
using Android.Content.PM;

namespace Sample.Push.Maui;


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
[IntentFilter(
    new[] {
        ShinyPushIntents.NotificationClickAction
    },
    Categories = new[] {        
        "android.intent.category.DEFAULT"
    }
)]
public class MainActivity : MauiAppCompatActivity
{
    //ShinyNotificationIntents.NotificationClickAction
}
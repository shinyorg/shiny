using Android.App;
using Android.Content.PM;
using Android.OS;

namespace Sample.Maui;

[Activity(
    LaunchMode = LaunchMode.SingleTop,
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
    [ 
        Shiny.ShinyPushIntents.NotificationClickAction,
        Shiny.ShinyNotificationIntents.NotificationClickAction
    ],
    Categories = [
        "android.intent.category.DEFAULT" 
    ]
)]
public class MainActivity : MauiAppCompatActivity
{
}

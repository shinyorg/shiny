using Shiny.Hosting;

namespace Sample.Dev;


#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;

public partial class PlatformStateTests : IAndroidLifecycle.IApplicationLifecycle,
                                          IAndroidLifecycle.IOnActivityNewIntent,
                                          IAndroidLifecycle.IOnActivityRequestPermissionsResult,
                                          IAndroidLifecycle.IOnActivityResult
{
    public void Handle(Activity activity, Intent intent) => this.Log("New Intent");
    public void Handle(Activity activity, int requestCode, string[] permissions, Permission[] grantResults) => this.Log("Permission Request");
    public void Handle(Activity activity, int requestCode, Result resultCode, Intent data) => this.Log("Activity Result");
    public void OnBackground() => this.Log("App Background");
    public void OnForeground() => this.Log("App Foreground");
}

#elif IOS || MACCATALYST
using Foundation;
using UIKit;
using UserNotifications;

public partial class PlatformStateTests : IIosLifecycle.IApplicationLifecycle,
                                          IIosLifecycle.IContinueActivity,
                                          IIosLifecycle.IHandleEventsForBackgroundUrl,
                                          IIosLifecycle.INotificationHandler,
                                          IIosLifecycle.IOnFinishedLaunching,
                                          IIosLifecycle.IRemoteNotifications
{
    public bool Handle(NSUserActivity activity, UIApplicationRestorationHandler completionHandler)
    {
        this.Log("NSUserActivity");
        return false;
    }

    public bool Handle(string sessionIdentifier, Action completionHandler)
    {
        this.Log("NSUserActivity");
        return false;
    }


    public void Handle(UIApplicationLaunchEventArgs _) => this.Log("Finished Launching");
    public void OnBackground() => this.Log("App Background");
    public void OnForeground() => this.Log("App Foreground");


    public void OnDidReceive(NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler) => this.Log("Push Received");
    public void OnDidReceiveNotificationResponse(UNNotificationResponse response, Action completionHandler) => this.Log("Notification Entry");
    public void OnFailedToRegister(NSError error) => this.Log("Failed to register for Push");
    public void OnRegistered(NSData deviceToken) => this.Log("Success Push Registration");
    public void OnWillPresentNotification(UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler) => this.Log("Presenting Notification");
}

#endif

public partial class PlatformStateTests
{
    readonly SampleSqliteConnection data;
    readonly ILogger logger;


    public PlatformStateTests(SampleSqliteConnection data, ILogger<PlatformStateTests> logger)
    {
        this.data = data;
        this.logger = logger;
    }


    void Log(string detail)
    {
        try
        {
            this.logger.LogDebug("Logging Event - " + detail);
            this.data.GetConnection().Insert(new Log
            {
                Category = "Platform",
                Detail = detail,
                Timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            this.logger.LogError("Failed to store event", ex);
        }
    }
}
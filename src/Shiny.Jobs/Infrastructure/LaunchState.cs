#if PLATFORM
#if IOS || MACCATALYST || TVOS
using CoreFoundation;
using Foundation;
using UIKit;
#elif ANDROID
using Android.App;
#endif

namespace Shiny.Jobs.Infrastructure;


static class LaunchState
{
    /// <summary>
    /// True when the OS started the process without bringing the app on screen - a BGTask, silent push or
    /// location launch on Apple, a WorkManager / service / broadcast start on Android
    /// </summary>
    internal static bool IsLaunchedInBackground()
    {
#if IOS || MACCATALYST || TVOS
        // ApplicationState is Inactive while a user launch finishes and Background for a background launch
        if (NSThread.IsMain)
            return UIApplication.SharedApplication.ApplicationState == UIApplicationState.Background;

        var state = UIApplicationState.Active;
        DispatchQueue.MainQueue.DispatchSync(() => state = UIApplication.SharedApplication.ApplicationState);
        return state == UIApplicationState.Background;
#elif ANDROID
        // anything less important than visible (service, cached, ...) means no activity is on its way up
        var info = new ActivityManager.RunningAppProcessInfo();
        ActivityManager.GetMyMemoryState(info);
        return info.Importance > Importance.Visible;
#else
        return false;
#endif
    }
}
#endif

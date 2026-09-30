using Android.App;
using Android.OS;

namespace Shiny.AppFunctions;

/// <summary>
/// Whether the user can see one of the app's activities. Counts started (not resumed) activities, so an agent
/// overlay such as Gemini's, which pauses the activity underneath without hiding it, still counts as in the app.
/// </summary>
sealed class AppVisibility : Java.Lang.Object, Application.IActivityLifecycleCallbacks
{
    static int installed;
    static int started;

    public static bool IsVisible => Volatile.Read(ref started) > 0;

    /// <summary>Must run before the first activity starts (the Shiny host is built in Application.OnCreate).</summary>
    public static void Install()
    {
        if (Interlocked.Exchange(ref installed, 1) == 0)
            ((Application)Application.Context).RegisterActivityLifecycleCallbacks(new AppVisibility());
    }

    public void OnActivityStarted(Activity activity) => Interlocked.Increment(ref started);
    public void OnActivityStopped(Activity activity) => Interlocked.Decrement(ref started);

    public void OnActivityCreated(Activity activity, Bundle? savedInstanceState) { }
    public void OnActivityResumed(Activity activity) { }
    public void OnActivityPaused(Activity activity) { }
    public void OnActivitySaveInstanceState(Activity activity, Bundle outState) { }
    public void OnActivityDestroyed(Activity activity) { }
}

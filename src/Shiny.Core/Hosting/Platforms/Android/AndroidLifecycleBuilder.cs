using System;
using System.Reactive.Subjects;
using Android.App;
using Android.OS;
using Android.Runtime;

namespace Shiny.Hosting;


[Preserve(AllMembers = true)]
public class AndroidLifecycleBuilder : Java.Lang.Object, Application.IActivityLifecycleCallbacks //, ILifecycleBuilder
{
    // TODO: only activity OnNewIntent & OnPermissionRequest need to be routed from the activity directly - the activity lifecycle callbacks can handle the rest
    public Subject<ActivityChanged> ActivitySubject { get; } = new();
    readonly WeakReference<Activity?> current = new(null);

    // TODO: events needed from the activity instance directly
    //public static void ShinyOnRequestPermissionsResult(this Activity activity, int requestCode, string[] permissions, Permission[] grantResults)
    //    => ShinyHost
    //        .Resolve<IPlatform>()
    //        .OnRequestPermissionsResult(requestCode, permissions, grantResults);

    //public static void ShinyOnActivityResult(this Activity activity, int requestCode, Result resultCode, Intent data)
    //    => ShinyHost
    //        .Resolve<IPlatform>()
    //        .OnActivityResult(requestCode, resultCode, data);

    //// no longer used
    //[Obsolete("No longer needed")]
    //public static void ShinyOnCreate(this Activity activity) { }
    ////=> activity.ShinyOnNewIntent(activity.Intent);

    //public static void ShinyOnNewIntent(this Activity activity, Intent? intent)
    //    => ShinyHost.Resolve<IPlatform>()?.OnNewIntent(intent);

    //public static IObservable<(Result Result, Intent Data)> RequestActivityResult<TActivity>(this IAndroidContext context) where TActivity : Activity
    //    => context.RequestActivityResult((requestCode, activity) =>
    //        activity.StartActivityForResult(typeof(TActivity), requestCode)
    //    );


    //[Preserve(AllMembers = true)]
    //public class AndroidLifecycle : ILifecycleObserver
    //{
    //    [Lifecycle.Event.OnResume]
    //    [Export]
    //    public void OnResume() { }


    //    [Lifecycle.Event.OnPause]
    //    [Export]
    //    public void OnPause() { }
    //}

    public Activity? Activity
    {
        get => this.current.TryGetTarget(out var a) ? a : null;
        set => this.current.SetTarget(value);
    }


    void Fire(Activity activity, ActivityState state, Bundle? bundle = null) => this.ActivitySubject.OnNext(new ActivityChanged(activity, state, bundle));


    public void OnActivityCreated(Activity activity, Bundle? savedInstanceState)
    {
        this.Activity = activity;
        this.Fire(activity, ActivityState.Created, savedInstanceState);
    }


    public void OnActivityPaused(Activity activity)
    {
        this.Activity = activity;
        this.Fire(activity, ActivityState.Paused);
    }


    public void OnActivityResumed(Activity activity)
    {
        this.Activity = activity;
        this.Fire(activity, ActivityState.Resumed);
    }


    public void OnActivityDestroyed(Activity activity)
        => this.Fire(activity, ActivityState.Destroyed);


    public void OnActivitySaveInstanceState(Activity activity, Bundle outState) => this.Fire(activity, ActivityState.SaveInstanceState, outState);
    public void OnActivityStarted(Activity activity) => this.Fire(activity, ActivityState.Started);
    public void OnActivityStopped(Activity activity) => this.Fire(activity, ActivityState.Stopped);
}

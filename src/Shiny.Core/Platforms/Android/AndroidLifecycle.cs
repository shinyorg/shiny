using System;
using System.Reactive.Subjects;
using Android.App;
using Android.OS;
using AndroidX.Lifecycle;

namespace Shiny.Hosting;


public class AndroidLifecycle : Java.Lang.Object, Application.IActivityLifecycleCallbacks, ILifecycleObserver, IDisposable
{
    public AndroidLifecycle(Application application)
    {
        application.RegisterActivityLifecycleCallbacks(this);
        //ProcessLifecycleOwner.Get().Lifecycle.AddObserver(this);
    }


    public Subject<ActivityChanged> ActivitySubject { get; } = new();
    readonly WeakReference<Activity?> current = new(null);

    //    [Lifecycle.Event.OnResume]
    //    [Export]
    //    public void OnResume() { }


    //    [Lifecycle.Event.OnPause]
    //    [Export]
    //    public void OnPause() { }

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


    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        //ProcessLifecycleOwner.Get().Lifecycle.AddObserver(this);  // remove
    }
}
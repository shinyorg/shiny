using System;
using System.Reactive.Subjects;
using Android.App;
using Android.OS;

namespace Shiny;


public class AndroidActivityLifecycle : Java.Lang.Object, Application.IActivityLifecycleCallbacks, IDisposable
{
    readonly Application application;


    public AndroidActivityLifecycle(Application application)
    {
        this.application = application;
        this.application.RegisterActivityLifecycleCallbacks(this);
    }


    public Subject<ActivityChanged> ActivitySubject { get; } = new();
    readonly WeakReference<Activity?> current = new(null);


    public Activity? Activity
    {
        get => this.current.TryGetTarget(out var a) ? a : null;
        private set => this.current.SetTarget(value);
    }


    void Fire(Activity activity, ActivityState state, Bundle? bundle = null) 
        => this.ActivitySubject.OnNext(new ActivityChanged(activity, state, bundle));


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
        this.application.UnregisterActivityLifecycleCallbacks(this);
        base.Dispose(disposing);
    }
}
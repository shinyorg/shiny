using System;
using System.Reactive.Subjects;
using Android.App;
using Android.OS;
using Android.Runtime;


namespace Shiny
{
    [Preserve(AllMembers = true)]
    class ActivityLifecycleCallbacks : Java.Lang.Object, Application.IActivityLifecycleCallbacks
    {
        public Subject<ActivityChanged> ActivitySubject { get; } = new Subject<ActivityChanged>();
        readonly WeakReference<Activity?> current = new WeakReference<Activity?>(null);


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
            this.Fire(activity, ActivityState.Paused);
        }


        public void OnActivityDestroyed(Activity activity)
            => this.Fire(activity, ActivityState.Destroyed);


        public void OnActivitySaveInstanceState(Activity activity, Bundle outState) => this.Fire(activity, ActivityState.SaveInstanceState, outState);
        public void OnActivityStarted(Activity activity) => this.Fire(activity, ActivityState.Started);
        public void OnActivityStopped(Activity activity) => this.Fire(activity, ActivityState.Stopped);
    }
}

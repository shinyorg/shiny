using System;
using Android.App;


namespace Shiny
{
    class ShinyTopActivity : ITopActivity, IShinyStartupTask
    {
        readonly Application app;
        readonly ActivityLifecycleCallbacks callbacks;


        public ShinyTopActivity(Application app)
        {
            this.app = app;
            this.callbacks = new ActivityLifecycleCallbacks();
        }


        public Activity? Current => this.callbacks.Activity;

        public void Start() => this.app.RegisterActivityLifecycleCallbacks(this.callbacks);

        public IObservable<ActivityChanged> WhenActivityStatusChanged() => this.callbacks.ActivitySubject;
    }
}

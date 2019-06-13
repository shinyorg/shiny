using System;
using Android.App;


namespace Shiny
{
    class ShinyTopActivity : ITopActivity
    {
        readonly ActivityLifecycleCallbacks callbacks;


        public ShinyTopActivity(Application app)
        {
            this.callbacks = new ActivityLifecycleCallbacks();
            app.RegisterActivityLifecycleCallbacks(this.callbacks);
        }


        public Activity Current => this.callbacks.Activity;
        public IObservable<ActivityChanged> WhenActivityStatusChanged() => this.callbacks.ActivitySubject;
    }
}

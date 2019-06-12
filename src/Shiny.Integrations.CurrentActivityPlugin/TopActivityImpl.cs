using System;
using System.Reactive.Linq;
using Android.App;
using Plugin.CurrentActivity;


namespace Shiny.Integrations.CurrentActivityPlugin
{
    class TopActivityImpl : ITopActivity
    {
        readonly bool autoInit;
        public TopActivityImpl(bool autoInit) => this.autoInit = autoInit;


        public void Init(Application app)
        {
            if (this.autoInit)
                CrossCurrentActivity.Current.Init(app);
        }


        public Activity Current => CrossCurrentActivity.Current.Activity;
        public IObservable<ActivityChanged> WhenActivityStatusChanged() => Observable.Create<ActivityChanged>(ob =>
        {
            var handler = new EventHandler<ActivityEventArgs>((sender, args) =>
                ob.OnNext(new ActivityChanged(args.Activity, args.Event.FromPlugin(), null))
            );
            CrossCurrentActivity.Current.ActivityStateChanged += handler;
            return () => CrossCurrentActivity.Current.ActivityStateChanged -= handler;
        });
    }
}

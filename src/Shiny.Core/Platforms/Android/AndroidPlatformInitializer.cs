using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.App;
using AndroidX.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;


namespace Shiny
{
    public class AndroidPlatformInitializer : Java.Lang.Object, ILifecycleObserver, IPlatformInitializer
    {
        readonly Subject<PlatformState> stateSubj = new Subject<PlatformState>();
        readonly Application app;


        public AndroidPlatformInitializer(Application app)
        {
            this.app = app;
            ProcessLifecycleOwner.Get().Lifecycle.AddObserver(this);
        }


        [Lifecycle.Event.OnResume]
        public void OnResume() => this.stateSubj.OnNext(PlatformState.Foreground);

        [Lifecycle.Event.OnPause]
        public void OnPause() => this.stateSubj.OnNext(PlatformState.Background);

        public IObservable<PlatformState> WhenStateChanged() => this.stateSubj
            .OnErrorResumeNext(Observable.Empty<PlatformState>());

        public void Register(IServiceCollection services)
        {
            services.AddSingleton(this.app);
            services.TryAddSingleton<AndroidContext>();
            services.TryAddSingleton<ITopActivity, ShinyTopActivity>();

            services.RegisterCommonServices();
        }
    }
}

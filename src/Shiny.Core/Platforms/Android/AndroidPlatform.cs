using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.App;
using AndroidX.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Infrastructure;
using Shiny.IO;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.Settings;


namespace Shiny
{
    public class AndroidPlatform : Java.Lang.Object, ILifecycleObserver, IPlatform
    {
        readonly Subject<PlatformState> stateSubj = new Subject<PlatformState>();
        readonly Application app;


        public AndroidPlatform(Application app)
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

            services.TryAddSingleton<ISerializer, ShinySerializer>();
            services.TryAddSingleton<IMessageBus, MessageBus>();
            services.TryAddSingleton<IRepository, FileSystemRepositoryImpl>();

            services.TryAddSingleton<IEnvironment, EnvironmentImpl>();
            services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
            services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
            services.TryAddSingleton<IJobManager, JobManager>();
            services.TryAddSingleton<IFileSystem, FileSystemImpl>();
            services.TryAddSingleton<ISettings, SettingsImpl>();
        }
    }
}

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.App;
using Android.Runtime;
using AndroidX.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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


        public static void Create(Application app)
        {
            ProcessLifecycleOwner.Get().Lifecycle.AddObserver(new AndroidPlatform());
        }


        [Lifecycle.Event.OnResume]
        public void OnResume() => this.stateSubj.OnNext(PlatformState.Foreground);

        [Lifecycle.Event.OnPause]
        public void OnPause() => this.stateSubj.OnNext(PlatformState.Background);

        public IObservable<PlatformState> WhenStateChanged() => this.stateSubj
            .OnErrorResumeNext(Observable.Empty<PlatformState>());

        public void Register(IServiceCollection services)
        {
            //services.AddSingleton(androidApp);
            //services.TryAddSingleton<AndroidContext>();
            services.TryAddSingleton<ITopActivity, ShinyTopActivity>();

            services.TryAddSingleton<IEnvironment, EnvironmentImpl>();
            services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
            services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
            services.TryAddSingleton<IJobManager, JobManager>();
            services.TryAddSingleton<IFileSystem, FileSystemImpl>();
            services.TryAddSingleton<ISettings, SettingsImpl>();
        }



        //public static void ShinyInit(this Application app, IShinyStartup? startup = null, Action<IServiceCollection>? platformBuild = null)
        //    => AndroidShinyHost.Init(app, startup, platformBuild);

        //public static void ShinyOnRequestPermissionsResult(this Activity activity, int requestCode, string[] permissions, Permission[] grantResults)
        //    => AndroidShinyHost.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        //public static void ShinyOnCreate(this Application application, IShinyStartup? startup = null, Action<IServiceCollection>? platformBuild = null)
        //    => AndroidShinyHost.Init(application, startup, platformBuild);

        //public static void ShinyOnCreate(this Activity activity)
        //    => AndroidShinyHost.TryProcessIntent(activity.Intent);

        //public static void ShinyOnNewIntent(this Activity activity, Intent intent)
        //    => AndroidShinyHost.TryProcessIntent(intent);


        //public static void TryProcessIntent(Intent intent)
        //{
        //    if (intent != null)
        //        Resolve<AndroidContext>()?.IntentSubject.OnNext(intent);
        //}


        //public static void OnBackground([GeneratedEnum] TrimMemory level)
        //{
        //    if (level == TrimMemory.UiHidden || level == TrimMemory.Complete)
        //        OnBackground();
        //}


        //public static void OnRequestPermissionsResult(int requestCode, string[] permissions, NativePerm[] grantResults)
        //    => Resolve<AndroidContext>().FirePermission(requestCode, permissions, grantResults);
    }
}

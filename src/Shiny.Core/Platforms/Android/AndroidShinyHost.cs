using System;
using Android.App;
using Shiny.IO;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.Settings;
using Microsoft.Extensions.DependencyInjection;
using NativePerm = Android.Content.PM.Permission;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Android.Runtime;
using Android.Content;


namespace Shiny
{
    public class AndroidShinyHost : ShinyHost
    {
        public static void Init(Application androidApp,
                                IShinyStartup? startup = null,
                                Action<IServiceCollection>? platformBuild = null)
        {
            InitPlatform(
                startup,
                services =>
                {
                    services.AddSingleton(androidApp);
                    services.TryAddSingleton<AndroidContext>();
                    services.TryAddSingleton<ITopActivity, ShinyTopActivity>();

                    services.TryAddSingleton<IEnvironment, EnvironmentImpl>();
                    services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
                    services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
                    services.TryAddSingleton<IJobManager, JobManager>();
                    services.TryAddSingleton<IFileSystem, FileSystemImpl>();
                    services.TryAddSingleton<ISettings, SettingsImpl>();
                    platformBuild?.Invoke(services);
                }
            );
        }


        public static void TryProcessIntent(Intent intent)
        {
            if (intent != null)
                Resolve<AndroidContext>()?.IntentSubject.OnNext(intent);
        }


        public static void OnBackground([GeneratedEnum] TrimMemory level)
        {
            if (level == TrimMemory.UiHidden || level == TrimMemory.Complete)
                OnBackground();
        }


        public static void OnRequestPermissionsResult(int requestCode, string[] permissions, NativePerm[] grantResults)
            => Resolve<AndroidContext>().FirePermission(requestCode, permissions, grantResults);
    }
}

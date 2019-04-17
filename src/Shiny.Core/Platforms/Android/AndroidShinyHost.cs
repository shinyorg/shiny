using System;
using Android.App;
using Shiny.Infrastructure;
using Shiny.IO;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.Settings;
using Microsoft.Extensions.DependencyInjection;
using NativePerm = Android.Content.PM.Permission;


namespace Shiny
{
    public class AndroidShinyHost : ShinyHost
    {
        public static void Init(Application androidApp,
                                IStartup startup = null,
                                Action<IServiceCollection> platformBuild = null)
        {
            var callbacks = new ActivityLifecycleCallbacks();
            androidApp.RegisterActivityLifecycleCallbacks(callbacks);
            var context = new AndroidContext(androidApp, callbacks);
            InitPlatform(
                startup,
                services =>
                {
                    services.AddSingleton(context);

                    services.AddSingleton<IEnvironment, EnvironmentImpl>();
                    services.AddSingleton<IConnectivity, ConnectivityImpl>();
                    services.AddSingleton<IPowerManager, PowerManagerImpl>();
                    services.AddSingleton<IJobManager, JobManager>();
                    services.AddSingleton<IRepository, FileSystemRepositoryImpl>();
                    services.AddSingleton<IFileSystem, FileSystemImpl>();
                    services.AddSingleton<ISerializer, JsonNetSerializer>();
                    services.AddSingleton<ISettings, SettingsImpl>();
                    platformBuild?.Invoke(services);
                }
            );
        }


        public static void OnRequestPermissionsResult(int requestCode, string[] permissions, NativePerm[] grantResults)
            => Resolve<AndroidContext>().FirePermission(requestCode, permissions, grantResults);
    }
}

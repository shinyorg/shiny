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

[assembly: UsesPermission(Android.Manifest.Permission.AccessNetworkState)]
[assembly: UsesPermission(Android.Manifest.Permission.BatteryStats)]
[assembly: UsesPermission(Android.Manifest.Permission.ReceiveBootCompleted)]

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


        public static void OnRequestPermissionsResult(int requestCode, string[] permissions, NativePerm[] grantResults)
            => Resolve<AndroidContext>().FirePermission(requestCode, permissions, grantResults);


        public static void OnForeground()
        {
        }


        public static void OnBackground()
        {
        }


        public static void OnAppTerminating()
        {
        }
    }
}

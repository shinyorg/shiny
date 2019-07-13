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
                                IShinyStartup startup = null,
                                Action<IServiceCollection> platformBuild = null)
        {
            InitPlatform(
                startup,
                services =>
                {
                    services.AddSingleton(androidApp);
                    services.AddSingleton<AndroidContext>();
                    services.AddSingleton<ITopActivity, ShinyTopActivity>();

                    services.AddSingleton<IEnvironment, EnvironmentImpl>();
                    services.AddSingleton<IConnectivity, ConnectivityImpl>();
                    services.AddSingleton<IPowerManager, PowerManagerImpl>();
                    services.AddSingleton<IJobManager, JobManager>();
                    services.AddSingleton<IFileSystem, FileSystemImpl>();
                    services.AddSingleton<ISettings, SettingsImpl>();
                    platformBuild?.Invoke(services);
                }
            );
        }


        public static void OnRequestPermissionsResult(int requestCode, string[] permissions, NativePerm[] grantResults)
            => Resolve<AndroidContext>().FirePermission(requestCode, permissions, grantResults);
    }
}

using System;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.IO;
using Shiny.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UIKit;


namespace Shiny
{
    public class iOSShinyHost : ShinyHost
    {
        public static void Init(IShinyStartup startup = null, Action<IServiceCollection> platformBuild = null)
            => InitPlatform(startup, services =>
            {
                services.TryAddSingleton<IEnvironment, EnvironmentImpl>();
                services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
                services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
                services.TryAddSingleton<IFileSystem, FileSystemImpl>();
                services.TryAddSingleton<ISettings, SettingsImpl>();

                //if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
                //{
                //    services.TryAddSingleton<IJobManager, BgTasksJobManager>();
                //}
                //else
                //{
                    services.TryAddSingleton<IJobManager, JobManager>();
                //}
                platformBuild?.Invoke(services);
            });
    }
}

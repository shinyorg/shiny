using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.IO;
using Shiny.Net;
using Shiny.Power;
using Shiny.Settings;


namespace Shiny
{
    public class WatchOsShinyHost : ShinyHost
    {
        //https://docs.microsoft.com/en-us/xamarin/ios/watchos/platform/background-tasks
        public static void Init(IShinyStartup startup = null, Action<IServiceCollection> platformBuild = null)
            => InitPlatform(startup, services =>
            {
                services.TryAddSingleton<IEnvironment, EnvironmentImpl>();
                services.TryAddSingleton<IConnectivity, SharedConnectivityImpl>();
                services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
                //services.TryAddSingleton<IJobManager, JobManager>();
                services.TryAddSingleton<IFileSystem, FileSystemImpl>();
                services.TryAddSingleton<ISettings, SettingsImpl>();
                platformBuild?.Invoke(services);
            });
    }
}

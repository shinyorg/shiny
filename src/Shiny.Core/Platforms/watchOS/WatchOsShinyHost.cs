using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.IO;
using Shiny.Net;
using Shiny.Settings;


namespace Shiny
{
    public class WatchOsShinyHost : ShinyHost
    {
        //https://docs.microsoft.com/en-us/xamarin/ios/watchos/platform/background-tasks
        public static void Init(IShinyStartup startup = null, Action<IServiceCollection> platformBuild = null)
            => InitPlatform(startup, services =>
            {
                services.AddSingleton<IEnvironment, EnvironmentImpl>();
                //services.AddSingleton<IConnectivity, ConnectivityImpl>();
                //services.AddSingleton<IPowerManager, PowerManagerImpl>();
                //services.AddSingleton<IJobManager, JobManager>();
                services.AddSingleton<IFileSystem, FileSystemImpl>();
                services.AddSingleton<ISettings, SettingsImpl>();
                platformBuild?.Invoke(services);
            });
    }
}

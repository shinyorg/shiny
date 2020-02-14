using System;
using Shiny.Net;
using Shiny.Power;
using Shiny.IO;
using Shiny.Settings;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public class MacShinyHost : ShinyHost
    {
        public static void Init(IShinyStartup? startup = null, Action<IServiceCollection>? platformBuild = null)
            => InitPlatform(startup, services =>
            {
                services.AddSingleton<IEnvironment, EnvironmentImpl>();
                services.AddSingleton<IConnectivity, SharedConnectivityImpl>();
                services.AddSingleton<IPowerManager, PowerManagerImpl>();
                //services.AddSingleton<IJobManager, JobManagerImpl>();
                services.AddSingleton<IFileSystem, FileSystemImpl>();
                services.AddSingleton<ISettings, SettingsImpl>();
                platformBuild?.Invoke(services);
            });
    }
}

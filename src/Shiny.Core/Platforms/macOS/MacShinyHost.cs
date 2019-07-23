using System;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.Infrastructure;
using Shiny.IO;
using Shiny.Settings;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public class MacShinyHost : ShinyHost
    {
        public static void Init(IStartup startup = null, Action<IServiceCollection> platformBuild = null)
            => InitPlatform(startup, services =>
            {
                services.TryAddSingleton<IEnvironment, EnvironmentImpl>();
                services.TryAddSingleton<IConnectivity, SharedConnectivityImpl>();
                services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
                services.TryAddSingleton<IJobManager, JobManagerImpl>();
                services.TryAddSingleton<IFileSystem, FileSystemImpl>();
                services.TryAddSingleton<ISettings, SettingsImpl>();
                platformBuild?.Invoke(services);
            });
    }
}

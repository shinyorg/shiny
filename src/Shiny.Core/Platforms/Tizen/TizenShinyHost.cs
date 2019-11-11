using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.IO;
using Shiny.Net;
using Shiny.Power;
using Shiny.Settings;


namespace Shiny
{
    public class TizenShinyHost : ShinyHost
    {
        public static void Init(IShinyStartup? startup = null, Action<IServiceCollection>? platformBuild = null)
            => InitPlatform(startup, services =>
            {
                services.TryAddSingleton<IEnvironment, EnvironmentImpl>();
                services.TryAddSingleton<IConnectivity, SharedConnectivityImpl>();
                services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
                //services.AddSingleton<IJobManager, JobManager>();
                services.TryAddSingleton<IFileSystem, FileSystemImpl>();
                services.AddSingleton<ISettings, SettingsImpl>();
                platformBuild?.Invoke(services);
            });
    }
}

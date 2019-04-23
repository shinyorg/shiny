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
    public class TvShinyHost : ShinyHost
    {
        public static void Init(IStartup startup = null, Action<IServiceCollection> platformBuild = null)
            => InitPlatform(startup, services =>
            {
                services.AddSingleton<IConnectivity, SharedConnectivityImpl>();
                services.AddSingleton<IPowerManager, PowerManagerImpl>();
                services.AddSingleton<IJobManager, JobManager>();
                services.AddSingleton<IRepository, FileSystemRepositoryImpl>();
                services.AddSingleton<IFileSystem, FileSystemImpl>();
                services.AddSingleton<ISerializer, JsonNetSerializer>();
                services.AddSingleton<ISettings, SettingsImpl>();
                platformBuild?.Invoke(services);
            });
    }
}

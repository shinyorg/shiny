using System;
using Shiny.Infrastructure;
using Shiny.IO;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.Settings;
using Microsoft.Extensions.DependencyInjection;
using Windows.ApplicationModel.Background;


namespace Shiny
{
    public class UwpShinyHost : ShinyHost
    {
        public static void Init<TBackgroundService>(IStartup startup = null, Action<IServiceCollection> platformBuild = null)
            where TBackgroundService : class, IBackgroundTask

            => InitPlatform(startup, services =>
            {
                BackgroundTaskTypeName = typeof(TBackgroundService).FullName;

                services.AddSingleton<IEnvironment, EnvironmentImpl>();
                services.AddSingleton<IConnectivity, ConnectivityImpl>();
                services.AddSingleton<IPowerManager, PowerManagerImpl>();
                services.AddSingleton<IRepository, FileSystemRepositoryImpl>();
                services.AddSingleton<IFileSystem, FileSystemImpl>();
                services.AddSingleton<ISerializer, JsonNetSerializer>();
                services.AddSingleton<ISettings, SettingsImpl>();
                services.AddSingleton<UwpContext>();

                services.AddSingleton<IJobManager, JobManager>();
                services.AddSingleton<IBackgroundTaskProcessor, JobBackgroundTaskProcessor>();

                platformBuild?.Invoke(services);
            });


        public static string BackgroundTaskTypeName { get; private set; }
        public static void Bridge(IBackgroundTaskInstance instanceTask) => Resolve<UwpContext>().Bridge(instanceTask);
    }
}

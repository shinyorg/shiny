using System;
using System.Linq;
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
        public static void Init(IStartup startup = null, Action<IServiceCollection> platformBuild = null)
            => InitPlatform(startup, services =>
            {
                services.AddSingleton<IEnvironment, EnvironmentImpl>();
                services.AddSingleton<IConnectivity, ConnectivityImpl>();
                services.AddSingleton<IPowerManager, PowerManagerImpl>();
                services.AddSingleton<IJobManager, JobManager>();
                services.AddSingleton<IRepository, FileSystemRepositoryImpl>();
                services.AddSingleton<IFileSystem, FileSystemImpl>();
                services.AddSingleton<ISerializer, JsonNetSerializer>();
                services.AddSingleton<ISettings, SettingsImpl>();
                platformBuild?.Invoke(services);
            });


        public static bool TryRegister(Type entryType, Action<BackgroundTaskBuilder> builderFunc)
        {
            var exists = BackgroundTaskRegistration
                .AllTasks
                .Where(x => x.Value.Name.Equals(entryType.FullName))
                .Any();

            if (exists)
                return false;

            var builder = new BackgroundTaskBuilder
            {
                Name = entryType.FullName,
                TaskEntryPoint = entryType.FullName
            };
            builderFunc(builder);
            builder.Register();
            return true;
        }


        public static bool TryUnRegister(Type entryType)
        {
            var task = BackgroundTaskRegistration
                .AllTasks
                .Where(x => x.Value.Name.Equals(entryType.FullName))
                .Select(x => x.Value)
                .FirstOrDefault();

            if (task == null)
                return false;

            task.Unregister(true);
            return true;
        }
    }
}

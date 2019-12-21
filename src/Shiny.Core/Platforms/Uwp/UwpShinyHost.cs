using System;
using System.Linq;
using Shiny.IO;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.Settings;
using Windows.Storage;
using Windows.ApplicationModel.Background;
using Microsoft.Extensions.DependencyInjection.Extensions;


namespace Shiny
{
    public class UwpShinyHost : ShinyHost
    {
        const string STARTUP_KEY = "ShinyStartupTypeName";
        const string MODULE_KEY = "ShinyPlatformModuleTypeName";
        static bool hydrated = false;


        public static void Init(IShinyStartup? startup = null, IShinyModule? platformModule = null)
        {
            if (!hydrated)
                InternalInit(startup, platformModule, false);
        }


        static void InternalInit(IShinyStartup? startup, IShinyModule? platformModule, bool fromBackground)
        {
            InitPlatform(startup, services =>
            {
                services.TryAddSingleton<IEnvironment, EnvironmentImpl>();
                services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
                services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
                services.TryAddSingleton<IFileSystem, FileSystemImpl>();
                services.TryAddSingleton<ISettings, SettingsImpl>();
                services.TryAddSingleton<IJobManager, JobManager>();

                if (platformModule != null)
                    services.RegisterModule(platformModule);

                if (!fromBackground)
                {
                    Dehydrate(STARTUP_KEY, startup);
                    Dehydrate(MODULE_KEY, platformModule);
                }
            });
            hydrated = true;
        }


        public static void BackgroundRun(IBackgroundTaskInstance taskInstance)
        {
            if (!hydrated)
            {
                var startup = Hydrate<IShinyStartup>(STARTUP_KEY);
                var module = Hydrate<IShinyModule>(MODULE_KEY);

                InternalInit(startup, module, true);
            }
            if (taskInstance.Task.Name.StartsWith("JOB-"))
            {
                UwpShinyHost
                    .Container
                    .ResolveOrInstantiate<JobBackgroundTaskProcessor>()
                    .Process(taskInstance);
            }
            else
            {

                var targetType = Type.GetType(taskInstance.Task.Name);
                var processor = UwpShinyHost.Container.ResolveOrInstantiate(targetType) as IBackgroundTaskProcessor;
                processor.Process(taskInstance);
            }
        }


        public static void RegisterBackground<TService>(Action<BackgroundTaskBuilder>? builderAction = null) where TService : IBackgroundTaskProcessor
        {
            var taskName = typeof(TService).AssemblyQualifiedName;
            if (GetTask(taskName) == null)
            {
                var builder = new BackgroundTaskBuilder();
                builder.Name = taskName;
                builder.TaskEntryPoint = typeof(ShinyBackgroundTask).FullName;

                builderAction?.Invoke(builder);
                builder.Register();
            }
        }


        public void UnRegisterBackground<TService>() where TService : IBackgroundTaskProcessor
            => GetTask(typeof(TService).AssemblyQualifiedName)?.Unregister(true);


        static IBackgroundTaskRegistration GetTask(string taskName) => BackgroundTaskRegistration
            .AllTasks
            .Where(x => x.Value.Name.Equals(taskName))
            .Select(x => x.Value)
            .FirstOrDefault();


        static void Dehydrate(string key, object? obj)
        {
            if (obj != null)
                ApplicationData.Current.LocalSettings.Values[key] = obj.GetType().AssemblyQualifiedName;
        }


        static T? Hydrate<T>(string key) where T : class
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            if (!settings.ContainsKey(key))
                return null;

            var typeName = settings[key].ToString();
            var type = Type.GetType(typeName);
            if (type != null)
            {
                var obj = Activator.CreateInstance(type) as T;
                return obj;
            }
            return null;
        }
    }
}

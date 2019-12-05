using System;
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


        public static void Init(IShinyStartup? startup = null, IShinyModule? platformModule = null)
            => InitPlatform(startup, services =>
            {
                services.TryAddSingleton<IEnvironment, EnvironmentImpl>();
                services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
                services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
                services.TryAddSingleton<IFileSystem, FileSystemImpl>();
                services.TryAddSingleton<ISettings, SettingsImpl>();
                services.TryAddSingleton<IJobManager, JobManager>();
                //services.TryAddSingleton<IBackgroundTaskProcessor, JobBackgroundTaskProcessor>();

                if (platformModule != null)
                {
                    services.RegisterModule(platformModule);
                    Dehydrate(MODULE_KEY, platformModule);
                }
                if (startup != null)
                    Dehydrate(STARTUP_KEY, startup);
            });


        static bool hydrated = false;

        public static void BackgroundRun(IBackgroundTaskInstance taskInstance)
        {
            if (!hydrated)
            {
                var startup = Hydrate<IShinyStartup>(STARTUP_KEY);
                var module = Hydrate<IShinyModule>(MODULE_KEY);

                Init(startup, module);
                hydrated = true;
            }
            //    var targetType(register.DelegateTypeName);
            //    var processor = this.serviceProvider.ResolveOrInstantiate(type) as IBackgroundTaskProcessor;
            //    processor?.Process(task.Task.Name, task); type = Type.G
        }


        //public async void RegisterBackground<TService>(string taskIdentifier, string backgroundTaskName, Action<BackgroundTaskBuilder>? builderAction = null) where TService : IBackgroundTaskProcessor
        //{
        //    // TODO: make sure the type isn't already registered - should do startup to reg tasks?
        //    var task = GetTask(taskIdentifier, typeof(TService));
        //    if (task != null)
        //        return;

        //    var builder = new BackgroundTaskBuilder();
        //    builderAction?.Invoke(builder);
        //    builder.Name = taskIdentifier;
        //    builder.TaskEntryPoint = backgroundTaskName;

        //    var registration = builder.Register();
        //    await this.repository.Set(registration.TaskId.ToString(), new UwpTaskRegister
        //    {
        //        TaskId = registration.TaskId,
        //        TaskName = taskIdentifier,
        //        DelegateTypeName = typeof(TService).AssemblyQualifiedName
        //    });
        //}


        //public void UnRegisterBackground<TService>(string taskIdentifier) where TService : IBackgroundTaskProcessor
        //    => GetTask(taskIdentifier, typeof(TService))?.Unregister(true);


        //static IBackgroundTaskRegistration GetTask(string taskIdentifier, Type serviceType) => BackgroundTaskRegistration
        //    .AllTasks
        //    .Where(x => x.Value.Name.Equals(taskIdentifier) || x.Value.Name.Equals(serviceType.FullName))
        //    .Select(x => x.Value)
        //    .FirstOrDefault();

        static void Dehydrate(string key, object obj)
        {
            if (obj == null)
                return;

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

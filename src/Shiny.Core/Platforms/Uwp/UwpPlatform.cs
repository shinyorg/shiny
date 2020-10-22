using System;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.IO;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.Settings;
using Windows.UI.Xaml;


namespace Shiny
{
    public class UwpPlatform : IPlatform
    {
        readonly Application app;
        public UwpPlatform(Application app) => this.app = app;


        public void Register(IServiceCollection services)
        {
            services.TryAddSingleton<ISerializer, ShinySerializer>();
            services.TryAddSingleton<IMessageBus, MessageBus>();
            services.TryAddSingleton<IRepository, FileSystemRepositoryImpl>();

            services.TryAddSingleton<IEnvironment, EnvironmentImpl>();
            services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
            services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
            services.TryAddSingleton<IFileSystem, FileSystemImpl>();
            services.TryAddSingleton<ISettings, SettingsImpl>();
            services.TryAddSingleton<IJobManager, JobManager>();

            //        if (platformModule != null)
            //            services.RegisterModule(platformModule);

            //        if (!fromBackground)
            //        {
            //            Dehydrate(STARTUP_KEY, startup);
            //            Dehydrate(MODULE_KEY, platformModule);
            //        }
            //    });
            //    hydrated = true;
        }

        public IObservable<PlatformState> WhenStateChanged() => Observable.Create<PlatformState>(ob =>
        {
            var fgHandler = new LeavingBackgroundEventHandler((sender, target) => ob.OnNext(PlatformState.Foreground));
            var bgHandler = new EnteredBackgroundEventHandler((sender, target) => ob.OnNext(PlatformState.Background));
            this.app.LeavingBackground += fgHandler;
            this.app.EnteredBackground += bgHandler;

            return () =>
            {
                this.app.LeavingBackground -= fgHandler;
                this.app.EnteredBackground -= bgHandler;
            };
        });
    }
}
//const string STARTUP_KEY = "ShinyStartupTypeName";
//const string MODULE_KEY = "ShinyPlatformModuleTypeName";
//static bool hydrated = false;


//public static void Init(Application app, IShinyStartup? startup = null, IShinyModule? platformModule = null)
//{
//    //app.Suspending +=


//    if (!hydrated)
//        InternalInit(startup, platformModule, false);
//}


//static void InternalInit(IShinyStartup? startup, IShinyModule? platformModule, bool fromBackground)
//{

//}


//public static void BackgroundRun(IBackgroundTaskInstance taskInstance)
//{
//    if (!hydrated)
//    {
//        var startup = Hydrate<IShinyStartup>(STARTUP_KEY);
//        var module = Hydrate<IShinyModule>(MODULE_KEY);

//        InternalInit(startup, module, true);
//    }
//    if (taskInstance.Task.Name.StartsWith("JOB-"))
//    {
//        UwpShinyHost
//            .Container
//            .ResolveOrInstantiate<JobManager>()
//            .Process(taskInstance);
//    }
//    else
//    {

//        var targetType = Type.GetType(taskInstance.Task.Name);
//        var processor = UwpShinyHost.Container.ResolveOrInstantiate(targetType) as IBackgroundTaskProcessor;
//        processor.Process(taskInstance);
//    }
//}


//public static void RegisterBackground<TService>(Action<BackgroundTaskBuilder>? builderAction = null) where TService : IBackgroundTaskProcessor
//{
//    var taskName = typeof(TService).AssemblyQualifiedName;
//    if (GetTask(taskName) == null)
//    {
//        var builder = new BackgroundTaskBuilder();
//        builder.Name = taskName;
//        builder.TaskEntryPoint = typeof(ShinyBackgroundTask).FullName;

//        builderAction?.Invoke(builder);
//        builder.Register();
//    }
//}


//public void UnRegisterBackground<TService>() where TService : IBackgroundTaskProcessor
//    => GetTask(typeof(TService).AssemblyQualifiedName)?.Unregister(true);


//public static void ClearBackgroundTasks() => BackgroundTaskRegistration
//        .AllTasks
//        .Select(x => x.Value)
//        .ToList()
//        .ForEach(x => x.Unregister(false));


//static IBackgroundTaskRegistration GetTask(string taskName) => BackgroundTaskRegistration
//    .AllTasks
//    .Where(x => x.Value.Name.Equals(taskName))
//    .Select(x => x.Value)
//    .FirstOrDefault();


//static void Dehydrate(string key, object? obj)
//{
//    if (obj != null)
//        ApplicationData.Current.LocalSettings.Values[key] = obj.GetType().AssemblyQualifiedName;
//}


//static T? Hydrate<T>(string key) where T : class
//{
//    var settings = ApplicationData.Current.LocalSettings.Values;
//    if (!settings.ContainsKey(key))
//        return null;

//    var typeName = settings[key].ToString();
//    var type = Type.GetType(typeName);
//    if (type != null)
//    {
//        var obj = Activator.CreateInstance(type) as T;
//        return obj;
//    }
//    return null;
//}
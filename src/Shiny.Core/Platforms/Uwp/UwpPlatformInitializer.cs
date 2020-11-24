using System;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;
using Shiny.Jobs;
//using Shiny.Support.Uwp;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Xaml;


namespace Shiny
{
    public class UwpPlatformInitializer : IStartupInitializer
    {
        public static string BackgroundTaskName => "TODO"; //typeof(Shiny.Support.Uwp.ShinyBackgroundTask).FullName;

        //const string STARTUP_KEY = "ShinyStartupTypeName";
        //static bool hydrated = false;
        readonly Application app;


        //private UwpPlatform() { }
        public UwpPlatformInitializer(Application app) => this.app = app;


        public void Register(IServiceCollection services)
        {
            services.RegisterCommonServices();
            // TODO: the modules and startup aren't piped through here, so I can't hydate/dehydrate calls
            // FROM INIT
            //    if (!hydrated)
            //        InternalInit(startup, platformModule, false);
        }



        public static void BackgroundRun(IBackgroundTaskInstance taskInstance)
        {
            //if (!hydrated)
            //{
            //    // TODO: I need to know what the startup type is!
            //    //var startup = Hydrate<IShinyStartup>(STARTUP_KEY);
            //    //ShinyHost.Init(new UwpPlatform(), startup);
            //}
            if (taskInstance.Task.Name.StartsWith("JOB-"))
            {
                ShinyHost
                    .Container
                    .ResolveOrInstantiate<JobManager>()
                    .Process(taskInstance);
            }
            else
            {

                var targetType = Type.GetType(taskInstance.Task.Name);
                var processor = ShinyHost.Container.ResolveOrInstantiate(targetType) as IBackgroundTaskProcessor;
                processor?.Process(taskInstance);
            }
        }


        public static void RegisterBackground<TService>(Action<BackgroundTaskBuilder>? builderAction = null) where TService : IBackgroundTaskProcessor
        {
            var taskName = typeof(TService).AssemblyQualifiedName;
            if (GetTask(taskName) == null)
            {
                var builder = new BackgroundTaskBuilder();
                builder.Name = taskName;
                builder.TaskEntryPoint = BackgroundTaskName;

                builderAction?.Invoke(builder);
                builder.Register();
            }
        }


        public void UnRegisterBackground<TService>() where TService : IBackgroundTaskProcessor
            => GetTask(typeof(TService).AssemblyQualifiedName)?.Unregister(true);


        public static void ClearBackgroundTasks() => BackgroundTaskRegistration
            .AllTasks
            .Select(x => x.Value)
            .ToList()
            .ForEach(x => x.Unregister(false));


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


        static IBackgroundTaskRegistration GetTask(string taskName) => BackgroundTaskRegistration
            .AllTasks
            .Where(x => x.Value.Name.Equals(taskName))
            .Select(x => x.Value)
            .FirstOrDefault();
    }
}






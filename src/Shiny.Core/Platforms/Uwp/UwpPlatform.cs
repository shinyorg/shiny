using System;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;
using Shiny.Support.Uwp;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Xaml;


namespace Shiny
{
    public class UwpPlatform : IPlatform
    {
        const string STARTUP_KEY = "ShinyStartupTypeName";
        const string MODULE_KEY = "ShinyPlatformModuleTypeName";
        static bool hydrated = false;
        readonly Application app;

        //private UwpPlatform() { }
        public UwpPlatform(Application app) => this.app = app;


        public void Register(IServiceCollection services)
        {
            services.RegisterCommonServices();
            // TODO: the modules and startup aren't piped through here, so I can't hydate/dehydrate calls
            // FROM INIT
            //    if (!hydrated)
            //        InternalInit(startup, platformModule, false);
        }


        //https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.Application?view=winrt-19041
        public IObservable<PlatformState> WhenStateChanged() => Observable.Create<PlatformState>(ob =>
        {
            var fgHandler = new LeavingBackgroundEventHandler((sender, target) => ob.OnNext(PlatformState.Foreground));
            var bgHandler = new EnteredBackgroundEventHandler((sender, target) => ob.OnNext(PlatformState.Background));

            if (this.app == null)
            {
                ob.OnNext(PlatformState.Background);
            }
            else
            {
                // TODO: application will be normal if launched from background
                this.app.LeavingBackground += fgHandler;
                this.app.EnteredBackground += bgHandler;
            }
            return () =>
            {
                if (this.app != null)
                {
                    this.app.LeavingBackground -= fgHandler;
                    this.app.EnteredBackground -= bgHandler;
                }
            };
        });


        public static void BackgroundRun(IBackgroundTaskInstance taskInstance)
        {
            if (!hydrated)
            {
                var startup = Hydrate<IShinyStartup>(STARTUP_KEY);
                var module = Hydrate<IShinyModule>(MODULE_KEY);

                // TODO: ShinyHost.Init(new UwpPlatform(), startup, module);
                //InternalInit(startup, module, true);
            }
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
                builder.TaskEntryPoint = typeof(ShinyBackgroundTask).FullName;

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






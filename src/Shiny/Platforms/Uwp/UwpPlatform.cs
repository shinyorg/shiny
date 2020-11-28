using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;
using Shiny.Infrastructure;


namespace Shiny
{
    public class UwpPlatform : IPlatform, IStartupInitializer
    {
        readonly EasClientDeviceInformation deviceInfo = new EasClientDeviceInformation();
        public static string BackgroundTaskName => typeof(Shiny.Support.Uwp.ShinyBackgroundTask).FullName;

        const string STARTUP_KEY = "ShinyStartupTypeName";
        static bool hydrated = false;
        readonly Application? app;


        public UwpPlatform(Application app) : this() => this.app = app;
        private UwpPlatform()
        {
            var path = ApplicationData.Current.LocalFolder.Path;
            this.AppData = new DirectoryInfo(path);
            this.Cache = new DirectoryInfo(Path.Combine(path, "Cache"));
            this.Public = new DirectoryInfo(Path.Combine(path, "Public"));
        }


        public DirectoryInfo AppData { get; }
        public DirectoryInfo Cache { get; }
        public DirectoryInfo Public { get; }

        public string AppIdentifier => Package.Current.Id.Name;
        public string AppVersion => $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}";
        public string AppBuild => Package.Current.Id.Version.Build.ToString();

        public string Manufacturer => this.deviceInfo.SystemManufacturer;
        public string Model => this.deviceInfo.SystemSku;
        public string OperatingSystem => "Windows"; //this.deviceInfo.OperatingSystem;
        public string OperatingSystemVersion => "";
        public string MachineName => "";


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


        public void Register(IServiceCollection services) => services.RegisterCommonServices();


        public void Initialize(IShinyStartup startup, IServiceCollection services)
        {
            startup.ConfigureServices(services);
            Dehydrate(STARTUP_KEY, startup);
        }


        public static void BackgroundRun(IBackgroundTaskInstance taskInstance)
        {
            if (!hydrated)
            {
                var startup = Hydrate<IShinyStartup>(STARTUP_KEY);
                ShinyHost.Init(new UwpPlatform(), startup);
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

using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;

namespace Shiny
{
    public class UwpPlatform : IPlatform
    {
        readonly EasClientDeviceInformation deviceInfo = new EasClientDeviceInformation();
        readonly Application? app;


        public UwpPlatform(Application? app = null)
        {
            this.app = app;
            var path = ApplicationData.Current.LocalFolder.Path;
            this.AppData = new DirectoryInfo(path);
            this.Cache = new DirectoryInfo(Path.Combine(path, "Cache"));
            this.Public = new DirectoryInfo(Path.Combine(path, "Public"));
        }


        public string Name => KnownPlatforms.Uwp;
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

        public PlatformState Status { get; private set; } = PlatformState.Foreground;


        public void InvokeOnMainThread(Action action)
        {
            var dispatcher = CoreApplication.MainView.CoreWindow?.Dispatcher;

            if (dispatcher == null)
                throw new NullReferenceException("Main thread missing");

            if (dispatcher.HasThreadAccess)
                action();
            else
                dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
        }


        //https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.Application?view=winrt-19041
        public IObservable<PlatformState> WhenStateChanged() => Observable
            .Create<PlatformState>(ob =>
            {
                var fgHandler = new LeavingBackgroundEventHandler((sender, target) => ob.OnNext(PlatformState.Foreground));
                var bgHandler = new EnteredBackgroundEventHandler((sender, target) => ob.OnNext(PlatformState.Background));

                if (this.app == null)
                {
                    ob.OnNext(PlatformState.Background);
                }
                else
                {
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
            })
            .Do(x => this.Status = x);


        public static bool RunInProc { get; set; }
        public static string? BackgroundTaskName { get; private set; }
        public static void SetBackgroundTask(Type backgroundTask)
            => BackgroundTaskName = $"{backgroundTask.Namespace}.{backgroundTask.Name}";


        public static void RegisterBackground<TService>(Action<BackgroundTaskBuilder>? builderAction = null) where TService : IBackgroundTaskProcessor
        {
            var taskName = typeof(TService).AssemblyQualifiedName;
            if (GetTask(taskName) == null)
            {
                var builder = new BackgroundTaskBuilder();
                builder.Name = taskName;
                if (!RunInProc)
                {
                    if (BackgroundTaskName.IsEmpty())
                        throw new ArgumentException("UwpPlatform.BackgroundTaskName has not been set properly. This would only happen if Shiny has not been bootstrapped properly");

                    builder.TaskEntryPoint = BackgroundTaskName;
                }
                builderAction?.Invoke(builder);
                builder.Register();
            }
        }


        public static void RunBackgroundTask(IBackgroundTask task, IBackgroundTaskInstance taskInstance, IShinyStartup startup)
        {
            if (!ShinyHost.IsInitialized)
            {
                UwpPlatform.SetBackgroundTask(task.GetType());
                ShinyHost.Init(new UwpPlatform(null), startup);
            }

            var services = ShinyHost.ServiceProvider;
            if (taskInstance.Task.Name.StartsWith("JOB-"))
            {
                services
                    .Resolve<JobManager>(true)!
                    .Process(taskInstance);
            }
            else
            {
                var targetType = Type.GetType(taskInstance.Task.Name);
                var processor = ActivatorUtilities.GetServiceOrCreateInstance(services, targetType) as IBackgroundTaskProcessor;
                processor?.Process(taskInstance);
            }
        }


        public void UnRegisterBackground<TService>() where TService : IBackgroundTaskProcessor
            => GetTask(typeof(TService).AssemblyQualifiedName)?.Unregister(true);


        public static IBackgroundTaskRegistration GetTask(string taskName) => BackgroundTaskRegistration
            .AllTasks
            .Where(x => x.Value.Name.Equals(taskName))
            .Select(x => x.Value)
            .FirstOrDefault();


        public static void RemoveBackgroundTask(string taskName)
        {
            // the task name for notifications is the processor, not the job name
            var taskNames = BackgroundTaskRegistration
                .AllTasks
                .Select(x => x.Value.Name)
                .ToList();

            var task = GetTask(taskName);
            if (task != null)
                task.Unregister(true);
        }


        public static void ClearBackgroundTasks() => BackgroundTaskRegistration
            .AllTasks
            .Select(x => x.Value)
            .ToList()
            .ForEach(x => x.Unregister(false));
    }
}

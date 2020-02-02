using System;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.IO;
using Shiny.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UIKit;
using Foundation;


namespace Shiny
{
    public class iOSShinyHost : ShinyHost
    {
        public static void Init(IShinyStartup? startup = null, Action<IServiceCollection>? platformBuild = null)
            => InitPlatform(startup, services =>
            {
                services.TryAddSingleton<IEnvironment, EnvironmentImpl>();
                services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
                services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
                services.TryAddSingleton<IFileSystem, FileSystemImpl>();
                services.TryAddSingleton<ISettings, SettingsImpl>();

                //if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
                //{
                //    services.TryAddSingleton<IJobManager, BgTasksJobManager>();
                //}
                //else
                //{
                    services.TryAddSingleton<IJobManager, JobManager>();
                //}
                platformBuild?.Invoke(services);
            });


        public static void RegisteredForRemoteNotifications(NSData deviceToken)
        {
            // PushManager.RegisteredForRemoteNotifications(deviceToken);
        }


        public static void FailedToRegisterForRemoteNotifications(NSError error)
        {
            // PushManager.FailedToRegisterForRemoteNotifications(error);
        }


        public static void PerformFetch(Action<UIBackgroundFetchResult> completionHandler)
        {
            //Resolve<IMessageBus>().Publish(new AppEvent)
            JobManager.OnBackgroundFetch(completionHandler);
        }


        public static void HandleEventsForBackgroundUrl(string sessionIdentifier, Action completionHandler)
        {
            // HttpTransferManager.SetCompletionHandler(sessionIdentifier, completionHandler);
        }


        public static void OnActivated()
        {
        }

        public static void OnTerminate()
        {
        }

        public static void OnForeground()
        {
        }

        public static void OnBackground()
        {
        }
    }
}

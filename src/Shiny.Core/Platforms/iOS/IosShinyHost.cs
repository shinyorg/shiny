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
        {
            InitPlatform(startup, services =>
            {
                services.TryAddSingleton<IEnvironment, EnvironmentImpl>();
                services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
                services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
                services.TryAddSingleton<IFileSystem, FileSystemImpl>();
                services.TryAddSingleton<ISettings, SettingsImpl>();
                //if (BgTasksJobManager.IsAvailable)
                //    services.TryAddSingleton<IJobManager, BgTasksJobManager>();
                //else
                    services.TryAddSingleton<IJobManager, JobManager>();

                platformBuild?.Invoke(services);
            });
            var app = UIApplication.SharedApplication;

            UIApplication.Notifications.ObserveWillEnterForeground(app, (_, __) => OnForeground());
            UIApplication.Notifications.ObserveDidEnterBackground(app, (_, __) => OnBackground());
        }


        static Action<NSData>? onRegisteredEvent;
        static Action<NSError>? onFailEvent;
        static Action<NSDictionary, Action<UIBackgroundFetchResult>>? onNotificationEvent;


        public static void RegisterForRemoteNotifications(Action<NSData> onRegistered,
                                                          Action<NSError> onFail,
                                                          Action<NSDictionary, Action<UIBackgroundFetchResult>> onNotification)
        {
            onRegisteredEvent = onRegistered;
            onFailEvent = onFail;
            onNotificationEvent = onNotification;
        }


        public static void DidReceiveRemoteNotification(NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
            => onNotificationEvent?.Invoke(userInfo, completionHandler ?? new Action<UIBackgroundFetchResult>(_ => { }));

        public static void RegisteredForRemoteNotifications(NSData deviceToken)
            => onRegisteredEvent?.Invoke(deviceToken);

        public static void FailedToRegisterForRemoteNotifications(NSError error)
            => onFailEvent?.Invoke(error);

        public static void PerformFetch(Action<UIBackgroundFetchResult> completionHandler)
            => JobManager.OnBackgroundFetch(completionHandler);

        public static Action<string, Action>? HandleEventsForBackgroundUrlAction { get; set; }
        public static void HandleEventsForBackgroundUrl(string sessionIdentifier, Action completionHandler)
            => HandleEventsForBackgroundUrlAction?.Invoke(sessionIdentifier, completionHandler);
    }
}

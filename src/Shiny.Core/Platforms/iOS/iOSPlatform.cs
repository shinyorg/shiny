using System;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Infrastructure;
using Shiny.IO;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.Settings;
using UIKit;


namespace Shiny
{
    public class iOSPlatform : IPlatform
    {
        public void Register(IServiceCollection services)
        {
            services.TryAddSingleton<ISerializer, ShinySerializer>();
            services.TryAddSingleton<IMessageBus, MessageBus>();
            services.TryAddSingleton<IEnvironment, EnvironmentImpl>();
            services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
            services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
            services.TryAddSingleton<IFileSystem, FileSystemImpl>();
            services.TryAddSingleton<ISettings, SettingsImpl>();

            if (BgTasksJobManager.IsAvailable)
                services.TryAddSingleton<IJobManager, BgTasksJobManager>();
            else
                services.TryAddSingleton<IJobManager, JobManager>();
        }


        public IObservable<PlatformState> WhenStateChanged() => Observable.Create<PlatformState>(ob =>
        {
            var fg = UIApplication.Notifications.ObserveWillEnterForeground(
                UIApplication.SharedApplication,
                (_, __) => ob.OnNext(PlatformState.Foreground)
            );
            var bg = UIApplication.Notifications.ObserveDidEnterBackground(
                UIApplication.SharedApplication,
                (_, __) => ob.OnNext(PlatformState.Background)
            );
            return () =>
            {
                fg?.Dispose();
                bg?.Dispose();
            };
        });


        //static Action<NSData>? onRegisteredEvent;
        //static Action<NSError>? onFailEvent;
        //static Action<NSDictionary, Action<UIBackgroundFetchResult>>? onNotificationEvent;


        //public static void RegisterForRemoteNotifications(Action<NSData> onRegistered,
        //                                                  Action<NSError> onFail,
        //                                                  Action<NSDictionary, Action<UIBackgroundFetchResult>> onNotification)
        //{
        //    onRegisteredEvent = onRegistered;
        //    onFailEvent = onFail;
        //    onNotificationEvent = onNotification;
        //}


        //public static void DidReceiveRemoteNotification(NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        //    => onNotificationEvent?.Invoke(userInfo, completionHandler ?? new Action<UIBackgroundFetchResult>(_ => { }));

        //public static void RegisteredForRemoteNotifications(NSData deviceToken)
        //    => onRegisteredEvent?.Invoke(deviceToken);

        //public static void FailedToRegisterForRemoteNotifications(NSError error)
        //    => onFailEvent?.Invoke(error);

        //public static void PerformFetch(Action<UIBackgroundFetchResult> completionHandler)
        //    => JobManager.OnBackgroundFetch(completionHandler);

        //public static Action<string, Action>? HandleEventsForBackgroundUrlAction { get; set; }
        //public static void HandleEventsForBackgroundUrl(string sessionIdentifier, Action completionHandler)
        //    => HandleEventsForBackgroundUrlAction?.Invoke(sessionIdentifier, completionHandler);

    }
}

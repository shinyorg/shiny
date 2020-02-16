using System;
using Shiny.Net;
using Shiny.Power;
using Shiny.IO;
using Shiny.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AppKit;
using Foundation;


namespace Shiny
{
    public class MacShinyHost : ShinyHost
    {
        public static void Init(IShinyStartup? startup = null, Action<IServiceCollection>? platformBuild = null)
        {
            InitPlatform(startup, services =>
            {
                services.TryAddSingleton<IEnvironment, EnvironmentImpl>();
                services.TryAddSingleton<IConnectivity, SharedConnectivityImpl>();
                services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
                //services.TryAddSingleton<IJobManager, JobManagerImpl>();
                services.TryAddSingleton<IFileSystem, FileSystemImpl>();
                services.TryAddSingleton<ISettings, SettingsImpl>();
                platformBuild?.Invoke(services);
            });
            //NSApplication.Notifications.ObserveApplicationActivated
            //NSApplication.Notifications.ObserveDidResignActive
            //NSApplication.Notifications.ObserveDidBecomeActive

        }


        public static void RegisteredForRemoteNotifications(NSData deviceToken)
        {

        }

        public static void FailedToRegisterForRemoteNotifications(NSError error)
        {

        }
    }
}

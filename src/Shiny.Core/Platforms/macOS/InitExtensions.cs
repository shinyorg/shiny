using System;
using AppKit;
using Foundation;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public static class InitExtensions
    {
        public static void ShinyDidFinishLaunching(this INSApplicationDelegate app, IShinyStartup? startup = null, Action<IServiceCollection>? platformBuild = null)
            => MacShinyHost.Init(startup, platformBuild);

        public static void ShinyRegisteredForRemoteNotifications(this INSApplicationDelegate app, NSData deviceToken)
            => MacShinyHost.RegisteredForRemoteNotifications(deviceToken);

        public static void ShinyFailedToRegisterForRemoteNotifications(this INSApplicationDelegate app, NSError error)
            => MacShinyHost.FailedToRegisterForRemoteNotifications(error);
    }
}

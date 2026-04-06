using AppKit;
using Foundation;
using Microsoft.Maui.Platform.MacOS.Hosting;
using ObjCRuntime;
using Shiny.Hosting;

namespace Sample.MacOS;

public class Program
{
    static void Main(string[] args)
    {
        NSApplication.Init();
        NSApplication.SharedApplication.Delegate = new MacOSMauiApplicationDelegate();
        NSApplication.Main(args);
    }
}

public class MacOSMauiApplicationDelegate : MacOSMauiApplication
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    public void DidRegisterForRemoteNotifications(NSApplication application, NSData deviceToken)
        => Host.Lifecycle.OnRegisteredForRemoteNotifications(deviceToken);

    [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
    public void DidFailToRegisterForRemoteNotifications(NSApplication application, NSError error)
        => Host.Lifecycle.OnFailedToRegisterForRemoteNotifications(error);

    [Export("application:didReceiveRemoteNotification:")]
    public void DidReceiveRemoteNotification(NSApplication application, NSDictionary userInfo)
        => Host.Lifecycle.OnDidReceiveRemoteNotification(userInfo);
}

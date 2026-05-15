using System;
using System.Collections.Generic;
using System.Linq;
using AppKit;
using Foundation;
using Microsoft.Extensions.Logging;
using UserNotifications;

namespace Shiny.Hosting;


public class MacLifecycleExecutor : IShinyStartupTask, IDisposable
{
    readonly ILogger logger;
    readonly IEnumerable<IMacLifecycle.IApplicationLifecycle> appHandlers;
    readonly IEnumerable<IMacLifecycle.IOnFinishedLaunching> finishLaunchingHandlers;
    readonly IEnumerable<IMacLifecycle.IContinueActivity> activityHandlers;
    readonly IEnumerable<IMacLifecycle.IRemoteNotifications> remoteHandlers;
    readonly IEnumerable<IMacLifecycle.INotificationHandler> notificationHandlers;
    readonly DisposableCollection disposer = new();


    public MacLifecycleExecutor(
        ILogger<MacLifecycleExecutor> logger,
        IEnumerable<IMacLifecycle.IApplicationLifecycle> appHandlers,
        IEnumerable<IMacLifecycle.IOnFinishedLaunching> finishLaunchingHandlers,
        IEnumerable<IMacLifecycle.IContinueActivity> activityHandlers,
        IEnumerable<IMacLifecycle.IRemoteNotifications> remoteHandlers,
        IEnumerable<IMacLifecycle.INotificationHandler> notificationHandlers
    )
    {
        this.logger = logger;
        this.appHandlers = appHandlers;
        this.finishLaunchingHandlers = finishLaunchingHandlers;
        this.activityHandlers = activityHandlers;
        this.remoteHandlers = remoteHandlers;
        this.notificationHandlers = notificationHandlers;
    }


    public void Start()
    {
        var center = NSNotificationCenter.DefaultCenter;

        this.disposer.Add(
            center.AddObserver(NSApplication.DidFinishLaunchingNotification, n =>
                this.Execute(this.finishLaunchingHandlers, h => h.Handle(n.UserInfo))
            )
        );

        this.disposer.Add(
            center.AddObserver(NSApplication.DidBecomeActiveNotification, _ =>
                this.Execute(this.appHandlers, h => h.OnForeground())
            )
        );

        this.disposer.Add(
            center.AddObserver(NSApplication.DidResignActiveNotification, _ =>
                this.Execute(this.appHandlers, h => h.OnBackground())
            )
        );

        if (this.notificationHandlers.Any())
        {
            if (UNUserNotificationCenter.Current.Delegate != null)
            {
                this.logger.LogWarning("UNUserNotificationCenter is already set. Shiny will not be able to run its notification delegates");
            }
            else
            {
                UNUserNotificationCenter.Current.Delegate = new ShinyUNUserNotificationCenterDelegate(
                    (response, completionHandler) => this.Execute(this.notificationHandlers, x => x.OnDidReceiveNotificationResponse(response, completionHandler)),
                    (notification, completionHandler) => this.Execute(this.notificationHandlers, x => x.OnWillPresentNotification(notification, completionHandler))
                );
            }
        }
    }


    public void OnRegisteredForRemoteNotifications(NSData deviceToken)
        => this.Execute(this.remoteHandlers, x => x.OnRegistered(deviceToken));


    public void OnFailedToRegisterForRemoteNotifications(NSError error)
        => this.Execute(this.remoteHandlers, x => x.OnFailedToRegister(error));


    public void OnDidReceiveRemoteNotification(NSDictionary userInfo)
        => this.Execute(this.remoteHandlers, x => x.OnDidReceive(userInfo));


    public bool OnContinueUserActivity(NSUserActivity userActivity)
        => this.HandleExecute(this.activityHandlers, x => x.Handle(userActivity));


    bool HandleExecute<T>(IEnumerable<T> services, Func<T, bool> func)
    {
        foreach (var handler in services)
        {
            try
            {
                if (func(handler))
                {
                    this.logger.LifecycleInfo(handler!.GetType().FullName!, typeof(T).FullName!);
                    return true;
                }
            }
            catch (Exception ex)
            {
                this.logger.LifecycleError(ex, handler!.GetType().FullName!, typeof(T).FullName!);
            }
        }
        return false;
    }


    void Execute<T>(IEnumerable<T> services, Action<T> action)
    {
        foreach (var handler in services)
        {
            try
            {
                this.logger.LifecycleInfo(handler!.GetType().FullName!, typeof(T).FullName!);
                action(handler);
            }
            catch (Exception ex)
            {
                this.logger.LifecycleError(ex, handler!.GetType().FullName!, typeof(T).FullName!);
            }
        }
    }


    public void Dispose()
    {
        this.disposer.Dispose();
        if (UNUserNotificationCenter.Current.Delegate is ShinyUNUserNotificationCenterDelegate)
            UNUserNotificationCenter.Current.Delegate = null;
    }
}

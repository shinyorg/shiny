﻿using System;
using System.Collections.Generic;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;

namespace Shiny.Hosting;


public class IosLifecycleExecutor
{
    readonly ILogger logger;
    readonly IEnumerable<IIosLifecycle.IApplicationLifecycle> appHandlers;
    readonly IEnumerable<IIosLifecycle.IOnFinishedLaunching> finishLaunchingHandlers;
    readonly IEnumerable<IIosLifecycle.IContinueActivity> activityHandlers;
    readonly IEnumerable<IIosLifecycle.IHandleEventsForBackgroundUrl> bgUrlHandlers;
    readonly IEnumerable<IIosLifecycle.IRemoteNotifications> remoteHandlers;


    public IosLifecycleExecutor(
        ILogger<IosLifecycleExecutor> logger,
        IEnumerable<IIosLifecycle.IApplicationLifecycle> appHandlers,
        IEnumerable<IIosLifecycle.IOnFinishedLaunching> finishLaunchingHandlers,
        IEnumerable<IIosLifecycle.IHandleEventsForBackgroundUrl> bgUrlHandlers,
        IEnumerable<IIosLifecycle.IContinueActivity> activityHandlers,
        IEnumerable<IIosLifecycle.IRemoteNotifications> remoteHandlers
    )
    {
        this.logger = logger;
        this.appHandlers = appHandlers;
        this.finishLaunchingHandlers = finishLaunchingHandlers;
        this.bgUrlHandlers = bgUrlHandlers;
        this.remoteHandlers = remoteHandlers;
        this.activityHandlers = activityHandlers;
    }


    public bool FinishedLaunching(NSDictionary options)
    { 
        this.Execute(this.finishLaunchingHandlers, handler => handler.Handle(options));
        return true;
    }

    public void OnRegisteredForRemoteNotifications(NSData deviceToken) 
        => this.Execute(this.remoteHandlers, x => x.OnRegistered(deviceToken));

    public void OnFailedToRegisterForRemoteNotifications(NSError error)
        => this.Execute(this.remoteHandlers, x => x.OnFailedToRegister(error));

    public void OnDidReceiveRemoveNotification(NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        => this.Execute(this.remoteHandlers, x => x.OnDidReceive(userInfo, completionHandler));

    public bool OnContinueUserActivity(NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
        => this.HandleExecute(this.activityHandlers, x => x.Handle(userActivity, completionHandler));

    public void OnAppForegrounding()
        => this.Execute(this.appHandlers, x => x.OnForeground());

    public void OnAppBackgrounding()
        => this.Execute(this.appHandlers, x => x.OnBackground());

    public bool OnHandleEventsForBackgroundUrl(string sessionIdentifier, Action completionHandler)
        => this.HandleExecute(this.bgUrlHandlers, x => x.Handle(sessionIdentifier, completionHandler));


    bool HandleExecute<T>(IEnumerable<T> services, Func<T, bool> func)
    {
        foreach (var handler in services)
        {
            try
            {
                if (func(handler))
                {
                    this.logger.LogDebug($"{handler!.GetType().FullName} handling lifecycle event for {typeof(T).FullName}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError("Failed to execute lifecycle call", ex);
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
                action(handler);
            }
            catch (Exception ex)
            {
                this.logger.LogError("Failed to execute lifecycle call", ex);
            }
        }
    }


    //public static bool ShinyHandleEventsForBackgroundUrl(this IUIApplicationDelegate _, string sessionUrl, Action completionHandler)
    //{
    //    var lifecycles = Host.Current.ServiceProvider.GetServices<IosLifecycle.IHandleEventsForBackgroundUrl>();
    //    var logger = Host.Current.Logging.CreateLogger<IIosLifecycle>();

    //    foreach (var lc in lifecycles)
    //    {
    //        try
    //        {
    //            // TODO: I'll need to pass in the completionhandler
    //            if (lc.Handle(sessionUrl))
    //                // TODO: handled, break loop and log
    //                return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            logger.LogError("Failed to execute lifecycle", ex);
    //        }
    //    }
    //    return false;
    //}


    //public static bool ShinyContinueActivity(NSUserActivity activity, UIApplicationRestorationHandler handler)
    //{
    //    var lifecycles = Host.Current.ServiceProvider.GetServices<IIosLifecycle.IContinueActivity>();
    //    var logger = Host.Current.Logging.CreateLogger<IosLifecycle>();

    //    foreach (var lc in lifecycles)
    //    {
    //        try
    //        {
    //            // TODO: must pass in handler
    //            if (lc.Handle(activity))
    //                // TODO: handled, break loop and log
    //                return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            logger.LogError("Failed to execute lifecycle", ex);
    //        }
    //    }
    //    return false;
    //}
}

using System;
using System.Collections.Generic;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;

namespace Shiny.Hosting;


public class IosLifecycleExecutor
{
    readonly ILogger logger;
    readonly IEnumerable<IIosLifecycle.IOnFinishedLaunching> finishLaunchingHandlers;
    readonly IEnumerable<IIosLifecycle.IHandleEventsForBackgroundUrl> bgUrlHandlers;


    public IosLifecycleExecutor(
        ILogger<IosLifecycleExecutor> logger,
        IEnumerable<IIosLifecycle.IOnFinishedLaunching> finishLaunchingHandlers,
        IEnumerable<IIosLifecycle.IHandleEventsForBackgroundUrl> bgUrlHandlers
    )
    {
        this.logger = logger;
        this.finishLaunchingHandlers = finishLaunchingHandlers;
        this.bgUrlHandlers = bgUrlHandlers;
    }


    public bool FinishedLaunching(NSDictionary options)
    { 
        this.Execute(this.finishLaunchingHandlers, handler => handler.Handle(options));
        return true;
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

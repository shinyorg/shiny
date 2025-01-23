using System;
using System.Reactive.Linq;
using System.IO;
using System.Linq;
using Foundation;

namespace Shiny;


public static class AppleShinyHost
{
    
}
/*
 public void Start()
   {
       UIApplication
           .Notifications
           .ObserveDidFinishLaunching((_, args) =>
               this.Execute(this.finishLaunchingHandlers, handler => handler.Handle(args))
           )
           .DisposedBy(this.disposer);

       UIApplication
           .Notifications
           .ObserveWillEnterForeground((_, _) => this.Execute(this.appHandlers, x => x.OnForeground()))
           .DisposedBy(this.disposer);

       UIApplication
           .Notifications
           .ObserveDidEnterBackground((_, _) => this.Execute(this.appHandlers, x => x.OnBackground()))
           .DisposedBy(this.disposer);


       if (this.notificationHandlers != null && this.notificationHandlers.Any())
       {
           if (UNUserNotificationCenter.Current.Delegate != null)
           {
               this.logger.LogWarning("UNUserNotificationCenter is already set.  Shiny will not be able to run its notification delegates");
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

   public void OnDidReceiveRemoteNotification(NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
       => this.Execute(this.remoteHandlers, x => x.OnDidReceive(userInfo, completionHandler));

   public bool OnContinueUserActivity(NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
       => this.HandleExecute(this.activityHandlers, x => x.Handle(userActivity, completionHandler));

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
   
   
public IosPlatform()
   {
       this.AppData = ToDirectory(NSSearchPathDirectory.LibraryDirectory);
       this.Public = ToDirectory(NSSearchPathDirectory.DocumentDirectory);
       this.Cache = ToDirectory(NSSearchPathDirectory.CachesDirectory);
   }

   static DirectoryInfo ToDirectory(NSSearchPathDirectory dir) => new DirectoryInfo(NSSearchPath.GetDirectories(dir, NSSearchPathDomain.User).First());
   public DirectoryInfo AppData { get; }
   public DirectoryInfo Cache { get; }
   public DirectoryInfo Public { get; }
   public string AppIdentifier => NSBundle.MainBundle.BundleIdentifier;

   //macCatalyst 13.0 = macOS 10.15 (Catalina)
   //macCatalyst 13.4 = macOS 10.15.4
   //macCatalyst 14.0 = macOS 11.0 (Big Sur)
   //macCatalyst 14.7 = macOS 11.6
   //macCatalyst 15.0 = macOS 12.0 (Monterey)
   //macCatalyst 15.3 = macOS 12.2 and 12.2.1
   //macCatalyst 15.4 = macOS 12.3
   //macCatalyst 15.5 = macOS 12.4
   //macCatalyst 15.6 = macOS 12.5
   public static bool IsAppleVersionAtleast(int osMajor, int osMinor = 0)
       => OperatingSystem.IsIOSVersionAtLeast(osMajor, osMinor) || OperatingSystem.IsMacCatalystVersionAtLeast(osMajor, osMinor);

   //public string AppVersion => NSBundle.MainBundle.InfoDictionary["CFBundleVersion"].ToString();
   //public string AppBuild => NSBundle.MainBundle.InfoDictionary["CFBundleShortVersionString"].ToString();

   //public string MachineName { get; } = "";
   //public string OperatingSystem => NSProcessInfo.ProcessInfo.OperatingSystemName;
   //public string OperatingSystemVersion => NSProcessInfo.ProcessInfo.OperatingSystemVersionString;
   //public string Manufacturer { get; } = "Apple";
   //public string Model { get; } = "";


   public void InvokeOnMainThread(Action action)
   {
       if (NSThread.Current.IsMainThread)
       {
           action();
       }
       else
       {
           NSRunLoop.Main.BeginInvokeOnMainThread(action);
       }
   }
 */

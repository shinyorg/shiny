using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;
using UserNotifications;


namespace Shiny
{
    /// <summary>
    /// Any services that are injecting this should also be marked with IShinyStartupTask
    /// </summary>
    public class AppleLifecycle : UNUserNotificationCenterDelegate
    {
        readonly ILogger logger;
        public AppleLifecycle(ILogger<AppleLifecycle> logger)
        {
            this.logger = logger;
            //var app = UIApplication.SharedApplication;

            //var selector = new ObjCRuntime.Selector("");
            ////app.RespondsToSelector(selector);
        }



        ShinyUserNotificationDelegate ndelegate;
        void EnsureNotificationDelegate()
        {
            this.ndelegate ??= new ShinyUserNotificationDelegate();
            UNUserNotificationCenter.Current.Delegate = this.ndelegate;
        }


        public IDisposable RegisterForNotificationReceived(Func<UNNotificationResponse, Task> task)
        {
            this.EnsureNotificationDelegate();
            return this.ndelegate.RegisterForNotificationReceived(task);
        }


        public IDisposable RegisterForNotificationPresentation(Func<UNNotification, Task> task)
        {
            this.EnsureNotificationDelegate();
            return this.ndelegate.RegisterForNotificationPresentation(task);
        }


        readonly List<Func<string, Action, bool>> handleEvents = new List<Func<string, Action, bool>>();
        public IDisposable RegisterHandleEventsForBackgroundUrl(Func<string, Action, bool> task)
        {
            this.handleEvents.Add(task);
            return Disposable.Create(() => this.handleEvents.Remove(task));
        }


        internal void HandleEventsForBackgroundUrl(string sessionIdentifier, Action completionHandler)
        {
            var events = this.handleEvents.ToList();

            foreach (var handler in events)
            {
                try
                {
                    if (handler(sessionIdentifier, completionHandler))
                        break; // done, there can only be one!
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "HandleEventsForBackgroundUrl");
                }
            }
            completionHandler();
        }


        readonly List<(Action<NSData> OnSuccess, Action<NSError> OnError)> remoteReg = new List<(Action<NSData> Success, Action<NSError> Error)>();
        public IDisposable RegisterForRemoteNotificationToken(Action<NSData> onSuccess, Action<NSError> onError)
        {
            var tuple = (onSuccess, onError);
            this.remoteReg.Add(tuple);
            return Disposable.Create(() => this.remoteReg.Remove(tuple));
        }


        internal void RegisteredForRemoteNotifications(NSData deviceToken)
        {
            var events = this.remoteReg.ToList();
            foreach (var reg in events)
            {
                try
                {
                    reg.OnSuccess(deviceToken);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "RegisteredForRemoteNotifications");
                }
            }
        }


        internal void FailedToRegisterForRemoteNotifications(NSError error)
        {
            var events = this.remoteReg.ToList();
            foreach (var reg in events)
            {
                try
                {
                    reg.OnError(error);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "FailedToRegisterForRemoteNotifications");
                }
            }
        }


        readonly List<Func<NSDictionary, Task>> receiveReg = new List<Func<NSDictionary, Task>>();
        public IDisposable RegisterToReceiveRemoteNotifications(Func<NSDictionary, Task> task)
        {
            this.receiveReg.Add(task);
            return Disposable.Create(() => this.receiveReg.Remove(task));
        }


        internal async void DidReceiveRemoteNotification(NSDictionary dictionary, Action<UIBackgroundFetchResult>? completionHandler)
        {
            var events = this.receiveReg.ToList();

            foreach (var reg in events)
            {
                try
                {
                    await reg.Invoke(dictionary);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "DidReceiveRemoteNotification");
                }
            }
            completionHandler?.Invoke(UIBackgroundFetchResult.NewData);
        }
    }
}
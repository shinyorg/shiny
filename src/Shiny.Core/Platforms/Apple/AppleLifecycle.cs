using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;


namespace Shiny
{
    /// <summary>
    /// Any services that are injecting this should also be marked with IShinyStartupTask
    /// </summary>
    public class AppleLifecycle
    {
        readonly ILogger logger;
        public AppleLifecycle(ILogger<AppleLifecycle> logger) => this.logger = logger;


        readonly List<Func<string, Action, bool>> handleEvents = new List<Func<string, Action, bool>>();
        public IDisposable RegisterHandleEventsForBackgroundUrl(Func<string, Action, bool> task)
        {
            this.handleEvents.Add(task);
            return Disposable.Create(() => this.handleEvents.Remove(task));
        }


        internal void HandleEventsForBackgroundUrl(string sessionIdentifier, Action completionHandler)
        {
            foreach (var handler in this.handleEvents)
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
            foreach (var reg in this.remoteReg)
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
            foreach (var reg in this.remoteReg)
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
            return Disposable.Create(() => this.receiveReg.Add(task));
        }


        internal async void DidReceiveRemoteNotification(NSDictionary dictionary, Action<UIBackgroundFetchResult> completionHandler)
        {
            foreach (var reg in this.receiveReg)
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
            completionHandler(UIBackgroundFetchResult.NewData);
        }
    }
}
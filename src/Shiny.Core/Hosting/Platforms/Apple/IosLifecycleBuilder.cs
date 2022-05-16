using System;
using Foundation;

namespace Shiny.Hosting;


public record FinishedLaunching(NSDictionary Options);
public record HandleEventsForBackgroundUrl(string SessionUrl);
public record RegisterContinueActivity(NSUserActivity Activity);


public static class LifecycleBuilderExtensions
{
    public static ILifecycleBuilder AddIos(this ILifecycleBuilder lifecycleBuilder, Action<IosLifecycleBuilder> builder)
    {
        return lifecycleBuilder;
    }
}


public class IosLifecycleBuilder
{
    public IosLifecycleBuilder OnFinishedLaunching(Action<FinishedLaunching> onLaunching)
    {
        return this;
    }


    public IosLifecycleBuilder OnHandleEventsForBackgroundUrl(Func<HandleEventsForBackgroundUrl, bool> onHandleEventsForBackgroundUrl)
    {
        return this;
    }


    public IosLifecycleBuilder OnContinueActivity(Func<RegisterContinueActivity, bool> onContinueActivity)
    {
        return this;
    }
}


//ShinyUserNotificationDelegate ndelegate;
//void EnsureNotificationDelegate()
//{
//    this.ndelegate ??= new ShinyUserNotificationDelegate();
//    UNUserNotificationCenter.Current.Delegate = this.ndelegate;
//}


//readonly List<Func<Task>> handleFetch = new List<Func<Task>>();
//public IDisposable RegisterForPerformFetch(Func<Task> task)
//{
//    this.handleFetch.Add(task);
//    return Disposable.Create(() => this.handleFetch.Remove(task));
//}


//readonly List<Action<NSDictionary>> finishLaunchers = new List<Action<NSDictionary>>();
//public IDisposable RegisterForOnFinishedLaunching(Action<NSDictionary> action)
//{
//    this.finishLaunchers.Add(action);
//    return Disposable.Create(() => this.finishLaunchers.Remove(action));
//}


//public IDisposable RegisterForNotificationReceived(Func<UNNotificationResponse, Task> task)
//{
//    this.EnsureNotificationDelegate();
//    return this.ndelegate.RegisterForNotificationReceived(task);
//}


//public IDisposable RegisterForNotificationPresentation(Func<UNNotification, Task> task)
//{
//    this.EnsureNotificationDelegate();
//    return this.ndelegate.RegisterForNotificationPresentation(task);
//}


//readonly List<Func<string, Action, bool>> handleEvents = new List<Func<string, Action, bool>>();
//public IDisposable RegisterHandleEventsForBackgroundUrl(Func<string, Action, bool> task)
//{
//    this.handleEvents.Add(task);
//    return Disposable.Create(() => this.handleEvents.Remove(task));
//}


//internal void OnFinishedLaunching(NSDictionary options)
//{
//    var events = this.finishLaunchers.ToList();

//    foreach (var handler in events)
//    {
//        try
//        {
//            handler(options);
//        }
//        catch (Exception ex)
//        {
//            this.logger.LogError(ex, "OnFinishedLaunching");
//        }
//    }
//}

//internal void HandleEventsForBackgroundUrl(string sessionIdentifier, Action completionHandler)
//{
//    var events = this.handleEvents.ToList();

//    foreach (var handler in events)
//    {
//        try
//        {
//            if (handler(sessionIdentifier, completionHandler))
//                break; // done, there can only be one!
//        }
//        catch (Exception ex)
//        {
//            this.logger.LogError(ex, "HandleEventsForBackgroundUrl");
//        }
//    }
//    completionHandler();
//}


//readonly List<Func<NSUserActivity, Task>> continueList = new List<Func<NSUserActivity, Task>>();
//public IDisposable RegisterContinueActivity(Func<NSUserActivity, Task> func)
//{
//    this.continueList.Add(func);
//    return Disposable.Create(() => this.continueList.Remove(func));
//}



//readonly List<(Action<NSData> OnSuccess, Action<NSError> OnError)> remoteReg = new List<(Action<NSData> Success, Action<NSError> Error)>();
//public IDisposable RegisterForRemoteNotificationToken(Action<NSData> onSuccess, Action<NSError> onError)
//{
//    var tuple = (onSuccess, onError);
//    this.remoteReg.Add(tuple);
//    return Disposable.Create(() => this.remoteReg.Remove(tuple));
//}


//internal void RegisteredForRemoteNotifications(NSData deviceToken)
//{
//    var events = this.remoteReg.ToList();
//    foreach (var reg in events)
//    {
//        try
//        {
//            reg.OnSuccess(deviceToken);
//        }
//        catch (Exception ex)
//        {
//            this.logger.LogError(ex, "RegisteredForRemoteNotifications");
//        }
//    }
//}


//internal bool ContinueUserActivity(NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
//{
//    if (this.continueList.Count == 0)
//        return false;

//    foreach (var func in this.continueList)
//    {
//        func.Invoke(userActivity).ContinueWith(_ => completionHandler.Invoke(null));
//    }
//    return true;
//}


//internal void FailedToRegisterForRemoteNotifications(NSError error)
//{
//    var events = this.remoteReg.ToList();
//    foreach (var reg in events)
//    {
//        try
//        {
//            reg.OnError(error);
//        }
//        catch (Exception ex)
//        {
//            this.logger.LogError(ex, "FailedToRegisterForRemoteNotifications");
//        }
//    }
//}


//readonly List<Func<NSDictionary, Task>> receiveReg = new List<Func<NSDictionary, Task>>();
//public IDisposable RegisterToReceiveRemoteNotifications(Func<NSDictionary, Task> task)
//{
//    this.receiveReg.Add(task);
//    return Disposable.Create(() => this.receiveReg.Remove(task));
//}


//internal async void DidReceiveRemoteNotification(NSDictionary dictionary, Action<UIBackgroundFetchResult>? completionHandler)
//{
//    var events = this.receiveReg.ToList();

//    foreach (var reg in events)
//    {
//        try
//        {
//            await reg.Invoke(dictionary);
//        }
//        catch (Exception ex)
//        {
//            this.logger.LogError(ex, "DidReceiveRemoteNotification");
//        }
//    }
//    completionHandler?.Invoke(UIBackgroundFetchResult.NewData);
//}



//internal async void OnPerformFetch(Action<UIBackgroundFetchResult> completionHandler)
//{
//    var events = this.handleFetch.ToList();

//    foreach (var reg in events)
//    {
//        try
//        {
//            await reg.Invoke();
//        }
//        catch (Exception ex)
//        {
//            this.logger.LogError(ex, "PerformFetch");
//        }
//    }
//    completionHandler?.Invoke(UIBackgroundFetchResult.NewData);
//}
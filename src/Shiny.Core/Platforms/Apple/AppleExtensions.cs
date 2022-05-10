using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using Foundation;
using UIKit;
using ObjCRuntime;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny;

public static class AppleExtensions
{

    static DateTime reference = new DateTime(2001, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);


    public static bool HasAppDelegateHook(string selector)
    {
        if (!selector.StartsWith("application:"))
            selector = "application:" + selector;

        if (!selector.EndsWith(":"))
            selector += ":";

        return UIApplication.SharedApplication.RespondsToSelector(new Selector(selector));
    }


    public static void AssertAppDelegateHook(string selector, string message)
    {
        if (!HasAppDelegateHook(selector))
            throw new InvalidProgramException(message);
    }


    public static DateTime ToDateTime(this NSDate date)
    {
        var utcDateTime = reference.AddSeconds(date.SecondsSinceReferenceDate);
        var dateTime = utcDateTime.ToLocalTime();
        return dateTime;
    }


    public static NSDate ToNSDate(this DateTime datetime)
    {
        var utcDateTime = datetime.ToUniversalTime();
        var date = NSDate.FromTimeIntervalSinceReferenceDate((utcDateTime - reference).TotalSeconds);
        return date;
    }


    public static IDictionary<string, string> FromNsDictionary(this NSDictionary ns)
    {
        var dict = new Dictionary<string, string>();
        if (ns != null)
        {
            foreach (var pair in ns)
                dict.Add(pair.Key.ToString(), pair.Value.ToString());
        }
        return dict;
    }


    public static NSDictionary ToNsDictionary(this IDictionary<string, string> dict)
        =>  NSDictionary.FromObjectsAndKeys(dict.Values.ToArray(), dict.Keys.ToArray());


    public static bool AssertInfoPlistEntry(string key, bool assert)
    {
        var contains = NSBundle.MainBundle.InfoDictionary.ContainsKey(new NSString(key));
        if (!contains)
        {
            if (assert)
                throw new ArgumentException($"You must set '{key}' in your Info.plist file");

            return false;
        }
        return true;
    }


    public static bool HasBackgroundMode(string bgMode)
    {
        var info = NSBundle.MainBundle.InfoDictionary;
        var key = new NSString("UIBackgroundModes");
        var mode = new NSString(bgMode);

        if (info.ContainsKey(key))
        {
            var array = info[key] as NSArray;
            if (array != null)
            {
                for (nuint i = 0; i < array.Count; i++)
                {
                    if (array.GetItem<NSString>(i) == mode)
                        return true;
                }
            }
        }
        return false;
    }


    public static bool HasPlistValue(string key, int? ifVersion = null)
    {
        if (ifVersion == null)
            return NSBundle.MainBundle.ObjectForInfoDictionary(key) != null;
#if __IOS__
        if (UIKit.UIDevice.CurrentDevice.CheckSystemVersion(ifVersion.Value, 0))
            return NSBundle.MainBundle.ObjectForInfoDictionary(key) != null;
#endif
        return false;
    }

    public static Guid ToGuid(this NSUuid uuid) => Guid.ParseExact(uuid.AsString(), "d");
    public static NSUuid ToNSUuid(this Guid guid) => new NSUuid(guid.ToString());


    public static void ShinyFinishedLaunching(this UIApplicationDelegate app, NSDictionary options)
        => ShinyHost.ServiceProvider.GetRequiredService<AppleLifecycle>().OnFinishedLaunching(options);

    public static void ShinyDidReceiveRemoteNotification(this UIApplicationDelegate app, NSDictionary userInfo, Action<UIBackgroundFetchResult>? completionHandler)
        => ShinyHost.ServiceProvider.GetRequiredService<AppleLifecycle>().DidReceiveRemoteNotification(userInfo, completionHandler);

    public static void ShinyRegisteredForRemoteNotifications(this UIApplicationDelegate app, NSData deviceToken)
        => ShinyHost.ServiceProvider.GetRequiredService<AppleLifecycle>().RegisteredForRemoteNotifications(deviceToken);

    public static void ShinyFailedToRegisterForRemoteNotifications(this UIApplicationDelegate app, NSError error)
        => ShinyHost.ServiceProvider.GetRequiredService<AppleLifecycle>().FailedToRegisterForRemoteNotifications(error);

    public static void ShinyPerformFetch(this UIApplicationDelegate app, Action<UIBackgroundFetchResult> completionHandler)
        => ShinyHost.ServiceProvider.GetRequiredService<AppleLifecycle>().OnPerformFetch(completionHandler);

    public static void ShinyHandleEventsForBackgroundUrl(this UIApplicationDelegate app, string sessionIdentifier, Action completionHandler)
        => ShinyHost.ServiceProvider.GetRequiredService<AppleLifecycle>().HandleEventsForBackgroundUrl(sessionIdentifier, completionHandler);

    public static bool ShinyContinueUserActivity(this UIApplicationDelegate app, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
        => ShinyHost.ServiceProvider.GetRequiredService<AppleLifecycle>().ContinueUserActivity(userActivity, completionHandler);
}
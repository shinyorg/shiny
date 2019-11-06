﻿using System;
using System.Collections.Generic;
using Foundation;
using UIKit;


namespace Shiny
{
    public static class PlatformExtensions
    {
        static DateTime reference = new DateTime(2001, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);


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


        public static NSDictionary ToNsDictionary(this IDictionary<string, string> dict)
        {
            var ns = new NSDictionary();
            foreach (var pair in dict)
                ns.SetValueForKey(new NSString(pair.Value), new NSString(pair.Key));

            return ns;
        }


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
                for (nuint i = 0; i < array.Count; i++)
                {
                    if (array.GetItem<NSString>(i) == mode)
                        return true;
                }
            }
            return false;
        }


        public static IDictionary<string, string> FromNsDictionary(this NSDictionary ns)
        {
            var dict = new Dictionary<string, string>();
            if (ns != null)
                foreach (var pair in ns)
                    dict.Add(pair.Key.ToString(), pair.Value.ToString());

            return dict;
        }


        //public static void Dispatch(this Action action)
        //{
        //    if (NSThread.Current.IsMainThread)
        //        action();
        //    else
        //        NSRunLoop.Main.BeginInvokeOnMainThread(action);
        //}


        public static bool HasPlistValue(string key, int? ifVersion = null)
        {
            if (ifVersion == null)
                return NSBundle.MainBundle.ObjectForInfoDictionary(key) != null;
#if __IOS__
            if (UIDevice.CurrentDevice.CheckSystemVersion(ifVersion.Value, 0))
                return NSBundle.MainBundle.ObjectForInfoDictionary(key) != null;
#endif
            return false;
        }

        public static Guid ToGuid(this NSUuid uuid) => Guid.ParseExact(uuid.AsString(), "d");
        public static NSUuid ToNSUuid(this Guid guid) => new NSUuid(guid.ToString());
    }
}

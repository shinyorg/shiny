﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using CoreLocation;
using Foundation;


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

        public static IDictionary<string, string> FromNsDictionary(this NSDictionary ns)
        {
            var dict = new Dictionary<string, string>();
            if (ns != null)
                foreach (var pair in ns)
                    dict.Add(pair.Key.ToString(), pair.Value.ToString());

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


        public static IObservable<AccessState> WhenAccessStatusChanged(this CLLocationManager locationManager, bool background)
            => ((ShinyLocationDelegate)locationManager.Delegate).WhenAccessStatusChanged(background);


        public static AccessState FromNative(this CLAuthorizationStatus status, bool background)
        {
            switch (status)
            {
                case CLAuthorizationStatus.Restricted:
                    return AccessState.Restricted;

                case CLAuthorizationStatus.Denied:
                    return AccessState.Denied;

                case CLAuthorizationStatus.AuthorizedWhenInUse:
                    return background ? AccessState.Restricted : AccessState.Available;

                case CLAuthorizationStatus.AuthorizedAlways:
                    return AccessState.Available;

                case CLAuthorizationStatus.NotDetermined:
                default:
                    return AccessState.Unknown;
            }
        }


        public static AccessState GetCurrentStatus(this CLLocationManager locationManager, bool background)
        {
            if (!CLLocationManager.LocationServicesEnabled)
                return AccessState.Disabled;

            return CLLocationManager.Status.FromNative(background);
        }


        public static AccessState GetCurrentStatus<T>(this CLLocationManager locationManager, bool background) where T : CLRegion
        {
#if __IOS__
            if (!CLLocationManager.IsMonitoringAvailable(typeof(T)))
                return AccessState.NotSupported;
#endif

            return locationManager.GetCurrentStatus(background);
        }


        public static async Task<AccessState> RequestAccess(this CLLocationManager locationManager, bool background)
        {
            var status = locationManager.GetCurrentStatus(background);
            if (status != AccessState.Unknown)
                return status;

            var task = locationManager
                .WhenAccessStatusChanged(background)
                .Take(1)
                .ToTask();

#if __IOS__
            if (background)
                locationManager.RequestAlwaysAuthorization();
            else
                locationManager.RequestWhenInUseAuthorization();
#elif !__TVOS__
            locationManager.RequestAlwaysAuthorization();
#endif

            status = await task;
            return status;
        }
    }
}

using System;
using CoreLocation;
using Foundation;


namespace Shiny
{
    public static class Permissions
    {
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
        //public Task<AccessState> BluetoothLe(BluetoothLeModes mode)
        //{
        //    //<string>bluetooth-peripheral</string>
        //    //<string>bluetooth-peripheral</string>
        //    //NSBluetoothPeripheralUsageDescription

        //    //if (!NSBundle.MainBundle.InfoDictionary.ContainsKey(new NSString("bluetooth-peripheral"))
        //    //  throw new ArgumentException("You must set 'bluetooth-peripheral' in
        //    return Task.FromResult(AccessState.Granted);
        //}


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
    }
}
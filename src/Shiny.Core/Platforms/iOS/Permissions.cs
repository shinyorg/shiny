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
    }
}
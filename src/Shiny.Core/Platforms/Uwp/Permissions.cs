using System;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Geolocation;


namespace Shiny
{
    public static class Permissions
    {
        static readonly object syncLock = new object();
        static string manifestContent;
        static string GetManifest()
        {
            if (manifestContent == null)
            {
                lock (syncLock)
                {
                    if (manifestContent == null)
                    {
                        manifestContent = File.ReadAllText("AppxManifest.xml");
                    }
                }
            }
            return manifestContent;
        }


        public static bool IsInMainfest(string permission)
        {
            var m = GetManifest();
            var s = $"<DeviceCapability Identifier=\"{permission}\"";
            var r = m.Contains(s);
            return r;
        }

        //public Task<AccessState> Notifications() => Task.FromResult(AccessState.Granted);

        //AccessState bleStatus;
        //public Task<AccessState> BluetoothLe(BluetoothLeModes mode) => Task.FromResult(this.bleStatus);

        //AccessState speechState;
        //public Task<AccessState> Speech() => Task.FromResult(this.speechState);


        //public Task<AccessState> Beacons(bool forMonitoring) =>
        //    this.BluetoothLe(BluetoothLeModes.Central | BluetoothLeModes.CentralBackground);





        //public async Task<AccessState> Location(bool background)
        //{
        //    var status = await Geolocator.RequestAccessAsync();

        //    switch (status)
        //    {
        //        case GeolocationAccessStatus.Allowed     : return AccessState.Granted;
        //        case GeolocationAccessStatus.Denied      : return AccessState.Denied;
        //        case GeolocationAccessStatus.Unspecified :
        //        default                                  : return AccessState.Unknown;
        //    }
        //}
    }
}
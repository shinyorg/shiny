using System;
using Android.Bluetooth;


namespace Shiny.BluetoothLE.Hosting
{
    public static class AndroidExtensions
    {
        public static GattStatus ToNative(this GattState status)
            => (GattStatus)Enum.Parse(typeof(GattStatus), status.ToString());
    }
}

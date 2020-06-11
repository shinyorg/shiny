using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Bluetooth;

namespace Shiny.BluetoothLE.Hosting
{
    public static class AndroidExtensions
    {
        public static GattStatus ToNative(this GattState status)
            => (GattStatus)Enum.Parse(typeof(GattStatus), status.ToString());
    }
}

using System;
using System.Linq;
using Android.Bluetooth;
using Java.Util;


namespace Shiny.BluetoothLE.Hosting
{
    public static class Constants
    {
        public static readonly Guid NotifyDescriptorId = new Guid("00002902-0000-1000-8000-00805f9b34fb");
        public static readonly UUID NotifyDescriptorUuid = UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");
        public static readonly byte[] NotifyEnableBytes = BluetoothGattDescriptor.EnableNotificationValue.ToArray();
        public static readonly byte[] NotifyDisableBytes = BluetoothGattDescriptor.DisableNotificationValue.ToArray();
        public static readonly byte[] IndicateEnableBytes = BluetoothGattDescriptor.EnableIndicationValue.ToArray();
    }
}

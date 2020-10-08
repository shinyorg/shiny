using System;


namespace Shiny.BluetoothLE.RefitClient
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class BleMethodAttribute : Attribute
    {
        public BleMethodAttribute(string serviceUuid, string characteristicUuid) { }
    }


    public class BleNotificationAttribute : BleMethodAttribute
    {
        public BleNotificationAttribute(string serviceUuid, string characteristicUuid, bool useIndicateIfAvailable)
            : base(serviceUuid, characteristicUuid) { }
    }
}

using System;


namespace Shiny.BluetoothLE.RefitClient
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class BleAttribute : Attribute
    {
        public BleAttribute(string serviceUuid, string characteristicUuid) { }
    }


    public class BleWriteAttribute : BleAttribute
    {
        public BleWriteAttribute(string serviceUuid, string characteristicUuid, bool transactional = false) : base(serviceUuid, characteristicUuid)
        {
        }
    }



    public class BleReadAttribute : BleAttribute
    {
        public BleReadAttribute(string serviceUuid, string characteristicUuid) : base(serviceUuid, characteristicUuid)
        {
        }
    }


    public class BleNotifyAttribute : BleAttribute
    {
        public BleNotifyAttribute(string serviceUuid, string characteristicUuid) : base(serviceUuid, characteristicUuid)
        {
        }
    }
}

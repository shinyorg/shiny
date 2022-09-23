using System;

namespace Shiny.BluetoothLE.Hosting.Managed;


[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class BleGattCharacteristicAttribute : Attribute
{
    public BleGattCharacteristicAttribute(string serviceUuid, string characteristicUuid)
    {
        this.ServiceUuid = serviceUuid;
        this.CharacteristicUuid = characteristicUuid;
    }


    public string ServiceUuid { get; }
    public string CharacteristicUuid { get; }

    // TODO: should this just be true for what we're doing here?
    //public bool Primary { get; set; } d
    public bool IsReadSecure { get; set; }
    public NotificationOptions Notifications { get; set; } = NotificationOptions.Notify;
    public WriteOptions Write { get; set; } = WriteOptions.WriteWithoutResponse;
}
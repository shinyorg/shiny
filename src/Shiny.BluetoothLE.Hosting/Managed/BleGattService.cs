using System;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Hosting.Managed;


[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class BleGattServiceAttribute : Attribute
{
    public BleGattServiceAttribute(string serviceUuid, string characteristicUuid)
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
    public WriteOptions Write { get; set; } = WriteOptions.Write;
}



public class BleGattService
{
    public IGattCharacteristic Characteristic { get; internal set; } = null!;

    public virtual Task<GattState> OnWrite(WriteRequest request)
        => throw new InvalidOperationException("");

    // if overridden, add read property & hook to this
    public virtual Task<ReadResult> OnRead(ReadRequest request)
        => throw new InvalidOperationException("");

    // if overridden, add notification property & hook to this
    public virtual Task OnSubscriptionChanged(IPeripheral peripheral, bool subscribed)
        => throw new InvalidOperationException("");
}


////// TODO: you can only have 1
//public interface IL2CapEndpointDelegate
//{
//    // TODO: secure?
//    // TODO: when this is done, do we close the channel?  users will likely loop on the thread or a timer/infinite thread of some sort
//    Task OnOpened(L2CapChannel channel);
//}
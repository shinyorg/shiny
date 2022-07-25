using System;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Hosting.Managed;


public class BleGattCharacteristic
{
    public IGattCharacteristic Characteristic { get; internal set; } = null!;

    public virtual Task<GattState> OnWrite(WriteRequest request)
        => throw new InvalidOperationException("This method must overridden to use write");

    // if overridden, add read property & hook to this
    public virtual Task<ReadResult> OnRead(ReadRequest request)
        => throw new InvalidOperationException("This method must overridden to use read");

    // if overridden, add notification property & hook to this
    public virtual Task OnSubscriptionChanged(IPeripheral peripheral, bool subscribed)
        => throw new InvalidOperationException("This method must overridden to use subscription");
}


////// TODO: you can only have 1
//public interface IL2CapEndpointDelegate
//{
//    // TODO: secure?
//    // TODO: when this is done, do we close the channel?  users will likely loop on the thread or a timer/infinite thread of some sort
//    Task OnOpened(L2CapChannel channel);
//}
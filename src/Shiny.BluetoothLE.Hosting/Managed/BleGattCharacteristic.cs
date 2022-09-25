using System;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Hosting.Managed;


public abstract class BleGattCharacteristic
{
    public IGattCharacteristic Characteristic { get; internal set; } = null!;
    protected int SubscriptionCount => this.Characteristic.SubscribedCentrals.Count;

    public virtual Task OnStart() => Task.CompletedTask;
    public virtual void OnStop() { }

    // if overridden, write cannot be overridden
    public virtual Task<GattResult> Request(WriteRequest request)
        => throw new InvalidOperationException("This method must overridden to use request");

    public virtual Task OnWrite(WriteRequest request)
        => throw new InvalidOperationException("This method must overridden to use write");

    // if overridden, add read property & hook to this
    public virtual Task<GattResult> OnRead(ReadRequest request)
        => throw new InvalidOperationException("This method must overridden to use read");

    // if overridden, add notification property & hook to this
    public virtual Task OnSubscriptionChanged(IPeripheral peripheral, bool subscribed)
        => throw new InvalidOperationException("This method must overridden to use subscription");
}
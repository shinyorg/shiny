using System;
using Shiny.BluetoothLE.Hosting;
using Shiny.BluetoothLE.Hosting.Managed;

namespace Sample.BleHosting;


[BleGattCharacteristic(Constants.ManagedServiceUuid, Constants.ManagedCharacteristicUuid)]
public class MyManagedCharacteristics : BleGattCharacteristic
{
    public MyManagedCharacteristics()
    {
    }

    public override Task OnStart() => base.OnStart();
    public override void OnStop() => base.OnStop();

    public override Task<ReadResult> OnRead(ReadRequest request) => base.OnRead(request);
    public override Task<GattState> OnWrite(WriteRequest request) => base.OnWrite(request);
    public override Task OnSubscriptionChanged(IPeripheral peripheral, bool subscribed) => base.OnSubscriptionChanged(peripheral, subscribed);
}
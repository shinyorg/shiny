using System;
using Shiny.BluetoothLE.Hosting;
using Shiny.BluetoothLE.Hosting.Managed;

namespace Sample.BleHosting;


[BleGattCharacteristic(Constants.ManagedServiceUuid, Constants.ManagedCharacteristicUuid)]
public class MyManagedCharacteristics : BleGattCharacteristic
{
    readonly SampleSqliteConnection conn;

    public MyManagedCharacteristics(SampleSqliteConnection conn)
        => this.conn = conn;

    //TODO
    public override Task OnStart() => base.OnStart();
    public override void OnStop() => base.OnStop();

    public override Task<GattResult> OnRead(ReadRequest request) => base.OnRead(request);
    public override Task OnWrite(WriteRequest request) => base.OnWrite(request);
    public override Task OnSubscriptionChanged(IPeripheral peripheral, bool subscribed) => base.OnSubscriptionChanged(peripheral, subscribed);
}


[BleGattCharacteristic(Constants.ManagedServiceUuid, Constants.ManagedCharacteristicRequestUuid)]
public class MyManagedRequestCharacteristic : BleGattCharacteristic
{
    public override Task<GattResult> Request(WriteRequest request)
    {
        return Task.FromResult(GattResult.Success(new byte[] { 0x01 }));
    }
}
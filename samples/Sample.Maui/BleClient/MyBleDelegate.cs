using Shiny.BluetoothLE;

namespace Sample.BleClient;


public class MyBleDelegate : BleDelegate
{
    readonly SampleSqliteConnection conn;
    public MyBleDelegate(SampleSqliteConnection conn)
        => this.conn = conn;


    public override Task OnAdapterStateChanged(AccessState state)
        => this.conn.Log(
            "BLE",
            "Adapter Status",
            $"New Status: {state}"
        );

    public override Task OnPeripheralStateChanged(IPeripheral peripheral)
        => this.conn.Log(
            "BLE",
            "Peripheral Connected",
            peripheral.Name
        );
}
using Shiny.BluetoothLE;

namespace Shiny.DeviceTests.Tests;


// adapter and peripheral changes reach the tests through the EventHub ("ble.adapter" / "ble.peripheral")
public class TestBleDelegate(EventHub events) : BleDelegate
{
    public override Task OnAdapterStateChanged(AccessState state)
    {
        events.Publish("ble.adapter", state);
        return Task.CompletedTask;
    }


    public override Task OnPeripheralStateChanged(IPeripheral peripheral)
    {
        events.Publish("ble.peripheral", new BlePeripheralChange(peripheral.Uuid, peripheral.Status));
        return Task.CompletedTask;
    }
}


public record BlePeripheralChange(string Uuid, ConnectionState Status);

using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE;

namespace Sample.Shared.Maui.Delegates;

public class SampleBleDelegate(ILogger<SampleBleDelegate> logger) : IBleDelegate
{
    public Task OnAdapterStateChanged(AccessState state)
    {
        logger.LogInformation("BLE Adapter State Changed: {State}", state);
        return Task.CompletedTask;
    }

    public Task OnPeripheralStateChanged(Shiny.BluetoothLE.IPeripheral peripheral)
    {
        logger.LogInformation("Peripheral State Changed: {Peripheral}", peripheral.Name);
        return Task.CompletedTask;
    }
}

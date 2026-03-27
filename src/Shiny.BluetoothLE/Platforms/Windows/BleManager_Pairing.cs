using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace Shiny.BluetoothLE;


public partial class BleManager : ICanViewPairedPeripherals
{
    public IReadOnlyList<IPeripheral> GetPairedPeripherals()
    {
        var selector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);

        var devices = DeviceInformation
            .FindAllAsync(selector)
            .AsTask()
            .GetAwaiter()
            .GetResult();

        var results = new List<IPeripheral>();
        foreach (var deviceInfo in devices)
        {
            try
            {
                var native = BluetoothLEDevice
                    .FromIdAsync(deviceInfo.Id)
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();

                if (native != null)
                    results.Add(this.GetPeripheral(native));
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to get paired peripheral: {DeviceId}", deviceInfo.Id);
            }
        }
        return results;
    }
}

using System.Collections.Generic;

namespace Shiny.BluetoothLE;


public partial class BleManager : ICanViewPairedPeripherals
{
    public IReadOnlyList<IPeripheral> GetPairedPeripherals() => throw new System.NotImplementedException();

    //BluetoothLEDevice.GetDeviceSelectorFromPairingState(true)
}

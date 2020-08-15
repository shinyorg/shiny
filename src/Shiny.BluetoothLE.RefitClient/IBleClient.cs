using System;


namespace Shiny.BluetoothLE.RefitClient
{
    public interface IBleClient
    {
        IPeripheral Peripheral { get; }
    }
}

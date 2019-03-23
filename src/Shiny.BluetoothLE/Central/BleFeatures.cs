using System;


namespace Shiny.BluetoothLE.Central
{
    [Flags]
    public enum BleFeatures
    {
        None = -1,
        ControlAdapterState = 1,
        OpenSettings = 2,
        ViewPairedPeripherals = 4,
        LowPoweredScan = 8,

        PairingRequests = 16,
        MtuRequests = 32,
        ReliableTransactions = 64
    }
}

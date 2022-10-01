using Android.Bluetooth.LE;

namespace Shiny.BluetoothLE;


public record AndroidScanConfig(
    ScanMode ScanMode = ScanMode.Balanced,


    /// <summary>
    /// Allows the use of Scan Batching, if supported by the underlying provider
    /// Currently, this only affects Android peripherals
    /// It defaults to false to be transparent/non-breaking with existing code
    /// </summary>
    bool UseScanBatching = false,

    params string[] ServiceUuids
) : ScanConfig(
    ServiceUuids
);

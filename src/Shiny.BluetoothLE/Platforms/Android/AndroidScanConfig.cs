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


    /// <summary>
    /// When true, the scanner reports both legacy AND Bluetooth 5 extended advertisements
    /// (setLegacy(false) + all-PHY scanning).  Defaults to false, which uses Android's
    /// default legacy scan - this is what virtually all BLE peripherals advertise with.
    /// Only enable this if you specifically need to discover devices using BT5 extended
    /// advertising; doing so unconditionally suppresses legacy advertisements on many
    /// chipsets, which makes most devices invisible.
    /// </summary>
    bool IncludeExtendedAdvertisements = false,

    params string[] ServiceUuids
) : ScanConfig(
    ServiceUuids
);

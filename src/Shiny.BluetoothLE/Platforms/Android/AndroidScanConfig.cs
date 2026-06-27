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
    /// When true (the default), the scanner reports both legacy AND Bluetooth 5 extended
    /// advertisements by enabling setLegacy(false) paired with all-PHY scanning - but ONLY
    /// on devices whose chipset actually supports LE extended advertising.  On devices that
    /// don't (or on Android &lt; 8), it transparently falls back to the legacy scan so the
    /// legacy advertisements that most peripherals send are never suppressed.
    /// Set to false to force a legacy-only scan.
    /// </summary>
    bool IncludeExtendedAdvertisements = true,

    params string[] ServiceUuids
) : ScanConfig(
    ServiceUuids
);

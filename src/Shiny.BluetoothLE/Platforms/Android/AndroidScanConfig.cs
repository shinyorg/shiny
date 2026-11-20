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
)
{
    /// <summary>
    /// Manufacturer data filters, each passed to ScanFilter.Builder.setManufacturerData. Like service
    /// UUID filters they are OR'd - an advertisement matching any filter is reported.
    /// </summary>
    /// <remarks>
    /// Android pauses unfiltered scans while the screen is off, so a scan that has to keep running in
    /// the background needs at least one filter. Use this when the peripheral advertises no service UUID.
    /// </remarks>
    public AndroidManufacturerDataFilter[] ManufacturerData { get; init; } = [];
}


/// <summary>
/// A manufacturer data scan filter.
/// </summary>
/// <param name="ManufacturerId">The Bluetooth SIG company identifier, e.g. 0x004C for Apple.</param>
/// <param name="Data">The bytes the manufacturer data (company identifier excluded) has to start with.</param>
/// <param name="Mask">Optional, same length as <paramref name="Data"/> - only the set bits are compared.</param>
public record AndroidManufacturerDataFilter(
    int ManufacturerId,
    byte[] Data,
    byte[]? Mask = null
);

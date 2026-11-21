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
    /// Manufacturer data scan filters. Like service UUID filters they are OR'd: an advertisement matching any
    /// service UUID or any manufacturer filter is reported.
    /// </summary>
    /// <remarks>
    /// Android stops delivering results from an unfiltered scan while the screen is off. Use this for a
    /// peripheral that advertises no service UUID, such as an iBeacon.
    /// </remarks>
    public AndroidManufacturerDataFilter[] ManufacturerData { get; init; } = [];
}


/// <summary>
/// Matches advertisements whose manufacturer data arrives under <paramref name="CompanyId"/> and starts with
/// <paramref name="DataPrefix"/>.
/// </summary>
/// <param name="CompanyId">The Bluetooth SIG company identifier, e.g. 0x004C for Apple.</param>
/// <param name="DataPrefix">The bytes the manufacturer data (company identifier excluded) has to start with. Empty matches any data.</param>
public record AndroidManufacturerDataFilter(ushort CompanyId, byte[] DataPrefix);

using System.Runtime.CompilerServices;

// AdvertisementServiceData and ManufacturerData moved to Shiny.BluetoothLE.Common so that the
// peripheral role can describe the payloads it advertises without taking a dependency on the
// central role. Same namespace, so nothing recompiles; the forwards keep already-compiled
// assemblies resolving against this one.
[assembly: TypeForwardedTo(typeof(Shiny.BluetoothLE.AdvertisementServiceData))]
[assembly: TypeForwardedTo(typeof(Shiny.BluetoothLE.ManufacturerData))]

namespace Shiny.BluetoothLE;

/// <summary>
/// Represents the data contained in a BLE advertisement packet
/// </summary>
public interface IAdvertisementData
{
    /// <summary>
    /// Gets the local name broadcast by the peripheral
    /// </summary>
    string? LocalName { get; }

    /// <summary>
    /// Gets whether the peripheral is connectable
    /// </summary>
    bool? IsConnectable { get; }

    /// <summary>
    /// Gets the service data entries in the advertisement
    /// </summary>
    AdvertisementServiceData[]? ServiceData { get; }

    /// <summary>
    /// Gets the manufacturer-specific data
    /// </summary>
    ManufacturerData? ManufacturerData { get; }

    /// <summary>
    /// Gets the advertised service UUIDs
    /// </summary>
    string[]? ServiceUuids { get; }

    /// <summary>
    /// Gets the transmit power level in dBm
    /// </summary>
    int? TxPower { get; }
}

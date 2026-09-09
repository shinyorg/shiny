using System.Collections.Generic;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Options used when starting BLE advertising from a hosted peripheral.
/// </summary>
/// <param name="LocalName">Optional local name included in the advertisement.</param>
/// <param name="ServiceUuids">GATT service UUIDs to advertise.</param>
public record AdvertisementOptions(
    string? LocalName = null,
    params string[] ServiceUuids
)
{
    /// <summary>
    /// Service data sections to include in the advertisement.
    /// </summary>
    /// <remarks>
    /// Not supported on Apple platforms - CoreBluetooth's <c>startAdvertising</c> accepts only a
    /// local name and a list of service UUIDs, and silently drops anything else. Setting this on
    /// iOS, Mac Catalyst or macOS throws rather than advertising a payload no one will receive.
    /// </remarks>
    public IReadOnlyList<AdvertisementServiceData> ServiceData { get; init; } = [];

    /// <summary>
    /// Manufacturer-specific data to include in the advertisement.
    /// </summary>
    /// <remarks>
    /// Not supported on Apple platforms, for the same reason as <see cref="ServiceData"/>. Use
    /// <see cref="IBleHostingManager.AdvertiseBeacon"/> for iBeacon, which Apple exposes through a
    /// dedicated path.
    /// </remarks>
    public ManufacturerData? ManufacturerData { get; init; }

    /// <summary>
    /// Whether the advertisement invites connections. Defaults to true.
    /// </summary>
    /// <remarks>
    /// Beacons set this to false: they broadcast one way and have no GATT server to connect to.
    /// Ignored on Apple platforms, which decide connectability from whether services are published.
    /// </remarks>
    public bool IsConnectable { get; init; } = true;

    /// <summary>
    /// Whether the advertisement includes the radio's transmit power level. Defaults to false.
    /// </summary>
    /// <remarks>Ignored on Apple platforms.</remarks>
    public bool IncludeTxPower { get; init; } = false;
}

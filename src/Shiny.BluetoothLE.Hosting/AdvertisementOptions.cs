namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Options used when starting BLE advertising from a hosted peripheral.
/// </summary>
/// <param name="LocalName">Optional local name included in the advertisement.</param>
/// <param name="ServiceUuids">GATT service UUIDs to advertise.</param>
public record AdvertisementOptions(
    string? LocalName = null,
    params string[] ServiceUuids
);

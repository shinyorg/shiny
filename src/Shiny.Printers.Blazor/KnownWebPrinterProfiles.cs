namespace Shiny.Printers.Blazor;


/// <summary>A GATT service/characteristic pair to probe when connecting over Web Bluetooth.</summary>
/// <param name="Name">Friendly profile name, surfaced as <see cref="BrowserPrinterConnection.Detail"/>.</param>
/// <param name="ServiceUuid">GATT service UUID that exposes the print characteristic (full 128-bit, lowercase).</param>
/// <param name="WriteCharacteristicUuid">
/// GATT characteristic the print stream is written to. When null the first writable characteristic on
/// the service is used.
/// </param>
public sealed record WebBluetoothProfile(string Name, string ServiceUuid, string? WriteCharacteristicUuid = null);


/// <summary>
/// The GATT service/characteristic combinations used by the overwhelming majority of cheap 58/80mm BLE
/// ESC/POS printers - the same fingerprints as <c>Shiny.Printers.BluetoothLE</c>, restated here so the
/// Blazor package carries no BLE-stack dependency. Extend <see cref="All"/> to register your own.
/// </summary>
/// <remarks>
/// Web Bluetooth requires full 128-bit UUIDs in lowercase; the 16-bit shorthand used elsewhere in Shiny
/// will not match.
/// </remarks>
public static class KnownWebPrinterProfiles
{
    // The classic ESC/POS BLE printer board: service 18F0 / write 2AF1.
    const string GenericService = "000018f0-0000-1000-8000-00805f9b34fb";
    const string GenericWrite = "00002af1-0000-1000-8000-00805f9b34fb";

    // HM-10 / CC254x style transparent UART module: FFE0 / FFE1.
    const string Hm10Service = "0000ffe0-0000-1000-8000-00805f9b34fb";
    const string Hm10Write = "0000ffe1-0000-1000-8000-00805f9b34fb";

    // Microchip/ISSC transparent UART (used by some Star/MTP units).
    const string IsscService = "49535343-fe7d-4ae5-8fa9-9fafd205e455";
    const string IsscWrite = "49535343-8841-43f4-a8d4-ecbe34729bb3";

    /// <summary>The registered profiles, in probe priority order. Mutable so apps can add their own.</summary>
    public static List<WebBluetoothProfile> All { get; } =
    [
        new("Generic ESC/POS (18F0/2AF1)", GenericService, GenericWrite),
        new("HM-10 UART (FFE0/FFE1)", Hm10Service, Hm10Write),
        new("ISSC UART (49535343)", IsscService, IsscWrite)
    ];
}

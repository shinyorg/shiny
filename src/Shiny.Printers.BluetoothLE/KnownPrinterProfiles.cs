using System.Collections.Generic;
using System.Linq;

namespace Shiny.Printers.BluetoothLE;


/// <summary>
/// A recognised BLE printer "fingerprint": the GATT service/characteristic combo it advertises plus a
/// sensible default config. Used to auto-match scan results to a <see cref="BlePrinterConfig"/>.
/// </summary>
public sealed record BlePrinterProfile(string Name, BlePrinterConfig Config);


/// <summary>
/// Built-in profiles for the GATT service/characteristic combinations used by the overwhelming majority
/// of cheap 58/80mm BLE ESC/POS printers. Extend <see cref="All"/> to register your own.
/// </summary>
public static class KnownPrinterProfiles
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

    /// <summary>The registered profiles, in match priority order. Mutable so apps can add their own.</summary>
    public static List<BlePrinterProfile> All { get; } = new()
    {
        new("Generic ESC/POS (18F0/2AF1)", new BlePrinterConfig
        {
            ServiceUuid = GenericService,
            WriteCharacteristicUuid = GenericWrite,
            Capabilities = PrinterCapabilities.Paper58mm
        }),
        new("HM-10 UART (FFE0/FFE1)", new BlePrinterConfig
        {
            ServiceUuid = Hm10Service,
            WriteCharacteristicUuid = Hm10Write,
            Capabilities = PrinterCapabilities.Paper58mm
        }),
        new("ISSC UART (49535343)", new BlePrinterConfig
        {
            ServiceUuid = IsscService,
            WriteCharacteristicUuid = IsscWrite,
            Capabilities = PrinterCapabilities.Paper58mm
        })
    };

    /// <summary>The service UUIDs to filter a scan on (required on iOS for background scanning).</summary>
    public static string[] ServiceUuids => All.Select(p => p.Config.ServiceUuid).Distinct().ToArray();

    /// <summary>Returns the first profile whose service UUID appears in <paramref name="advertisedServiceUuids"/>, or null.</summary>
    public static BlePrinterProfile? Match(IEnumerable<string>? advertisedServiceUuids)
    {
        if (advertisedServiceUuids is null)
            return null;

        var advertised = advertisedServiceUuids
            .Where(x => x is not null)
            .Select(x => x.ToLowerInvariant())
            .ToHashSet();

        return All.FirstOrDefault(p => advertised.Contains(p.Config.ServiceUuid.ToLowerInvariant()));
    }
}

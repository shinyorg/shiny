using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Shiny.BluetoothLE.Hosting.Infrastructure;


/// <summary>
/// Measures what an <see cref="AdvertisementOptions"/> needs in a legacy BLE advertising packet and fails before the
/// radio is touched when it does not fit. Every platform advertises through the legacy (Bluetooth 4.x) PDU, whose
/// payload is 31 bytes. Without this check an oversized advertisement fails late and differently everywhere: Android
/// reports ADVERTISE_FAILED_DATA_TOO_LARGE through a callback, BlueZ refuses the registration, Windows aborts the
/// publisher, and CoreBluetooth quietly moves the UUIDs that do not fit into its overflow area, where only an iOS
/// device scanning for them by UUID can see them.
/// </summary>
/// <remarks>
/// Compiled on every target framework so it is unit tested on net11.0; each platform only says which sections it
/// puts in the packet (<see cref="PacketLayout"/>). <see cref="AdvertisementOptions.LocalName"/> is never counted:
/// CoreBluetooth and BlueZ move it to the scan response or truncate it, and Android does not put it in the packet.
/// </remarks>
static class AdvertisementPacket
{
    /// <summary>The payload of a legacy advertising PDU, in bytes.</summary>
    public const int MaxLength = 31;

    // every AD structure is a length byte and a type byte, then the data
    const int HeaderLength = 2;
    const string BaseUuidTail = "-0000-1000-8000-00805f9b34fb";


    /// <summary>
    /// Throws an <see cref="ArgumentException"/> naming each section and its size when the advertisement needs more
    /// than <see cref="MaxLength"/> bytes.
    /// </summary>
    public static void EnsureFits(AdvertisementOptions options, PacketLayout layout)
    {
        var sections = Measure(options, layout);
        var total = sections.Sum(x => x.Length);
        if (total <= MaxLength)
            return;

        var breakdown = String.Join(", ", sections.Select(x => $"{x.Name} {x.Length}"));
        throw new ArgumentException(
            $"The advertisement needs {total} bytes but a BLE advertising packet holds {MaxLength} ({breakdown}). " +
            "A 128-bit service UUID takes 16 bytes, so two of them never fit - advertise one UUID (or 16-bit SIG UUIDs), " +
            "trim the service or manufacturer data, and let centrals discover the rest of your services from the GATT server after connecting.",
            nameof(options)
        );
    }


    /// <summary>
    /// The AD structures the advertisement turns into, with their encoded length (header included).
    /// </summary>
    public static IReadOnlyList<(string Name, int Length)> Measure(AdvertisementOptions options, PacketLayout layout)
    {
        var sections = new List<(string Name, int Length)>();

        if (layout.Flags)
            sections.Add(("flags", HeaderLength + 1));

        // one list per UUID size - every platform groups them that way
        foreach (var group in options.ServiceUuids.GroupBy(GetUuidLength).OrderBy(x => x.Key))
        {
            var count = group.Count();
            var label = count == 1 ? "UUID" : "UUIDs";
            sections.Add(($"{count} {group.Key * 8}-bit service {label}", HeaderLength + group.Key * count));
        }

        foreach (var sd in options.ServiceData)
            sections.Add(($"service data {sd.Uuid}", HeaderLength + GetUuidLength(sd.Uuid) + sd.Data.Length));

        if (options.ManufacturerData != null)
            sections.Add(("manufacturer data", HeaderLength + 2 + options.ManufacturerData.Data.Length));

        if (layout.TxPower && options.IncludeTxPower)
            sections.Add(("tx power", HeaderLength + 1));

        return sections;
    }


    /// <summary>
    /// 2 bytes for a UUID in the Bluetooth base range with a zero upper half (0000xxxx-...), 4 for the rest of the
    /// base range, 16 for anything else - including a value that does not parse, which the platform rejects itself.
    /// </summary>
    public static int GetUuidLength(string uuid)
    {
        var value = uuid.Trim();
        if (value.Length == 4 && UInt16.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
            return 2;

        if (value.Length == 8 && UInt32.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var shortId))
            return shortId <= UInt16.MaxValue ? 2 : 4;

        if (!Guid.TryParse(value, out var guid))
            return 16;

        var text = guid.ToString("D");
        if (!text.EndsWith(BaseUuidTail, StringComparison.Ordinal))
            return 16;

        return text.StartsWith("0000", StringComparison.Ordinal) ? 2 : 4;
    }
}


/// <summary>
/// The sections a platform writes into the advertising packet itself, beyond the service UUIDs, service data and
/// manufacturer data that always go there.
/// </summary>
/// <param name="Flags">The platform adds a 3-byte flags structure.</param>
/// <param name="TxPower">The platform honours <see cref="AdvertisementOptions.IncludeTxPower"/>.</param>
readonly record struct PacketLayout(bool Flags, bool TxPower);

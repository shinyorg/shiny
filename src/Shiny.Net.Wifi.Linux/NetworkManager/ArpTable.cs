using System.Net;

namespace Shiny.Net.Wifi.NetworkManager;


/// <summary>
/// Reads the kernel's IPv4 neighbour table out of <c>/proc/net/arp</c>.
/// </summary>
/// <remarks>
/// The file is a fixed-column table: IP address, HW type, flags, HW address, mask, device. It is
/// world-readable on Linux (unlike Android, which locked it down in Android 10) and is the only way
/// to see who is on an AP-mode interface, since NetworkManager does not report stations.
/// </remarks>
internal static class ArpTable
{
    const string Path = "/proc/net/arp";

    /// <summary>NUD_INCOMPLETE - an entry still being resolved, with no usable MAC yet.</summary>
    const int IncompleteFlag = 0x0;

    public static IReadOnlyList<ArpEntry> Read(string interfaceName)
    {
        if (!File.Exists(Path))
            return Array.Empty<ArpEntry>();

        try
        {
            return File
                .ReadLines(Path)
                .Skip(1) // the column header
                .Select(Parse)
                .Where(x => x != null && x.Device == interfaceName)
                .ToArray()!;
        }
        catch (IOException)
        {
            // procfs entries can vanish mid-read; an empty client list beats throwing
            return Array.Empty<ArpEntry>();
        }
    }


    static ArpEntry? Parse(string line)
    {
        var columns = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (columns.Length < 6)
            return null;

        if (!IPAddress.TryParse(columns[0], out var address))
            return null;

        var flags = ParseHex(columns[2]);
        if (flags == IncompleteFlag)
            return null;

        return new ArpEntry(address, columns[3], columns[5]);
    }


    static int ParseHex(string value)
        => value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
           Int32.TryParse(value.AsSpan(2), System.Globalization.NumberStyles.HexNumber, null, out var parsed)
            ? parsed
            : 0;
}


internal sealed record ArpEntry(IPAddress Address, string MacAddress, string Device);

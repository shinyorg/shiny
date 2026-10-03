using System.Collections.Generic;

namespace Shiny.Printers.Network;


/// <summary>The DNS-SD service types networked thermal/receipt printers advertise themselves under.</summary>
public static class KnownNetworkPrinterServiceTypes
{
    /// <summary>Raw "RAW" / JetDirect stream, port 9100 - what ESC/POS receipt printers use.</summary>
    public const string PdlDatastream = "_pdl-datastream._tcp";

    /// <summary>LPR/LPD line-printer daemon, port 515.</summary>
    public const string LinePrinter = "_printer._tcp";

    /// <summary>The types scanned by default, in priority order.</summary>
    public static IReadOnlyList<string> All { get; } = [PdlDatastream, LinePrinter];
}

using System.Collections.Generic;
using System.Net;

namespace Shiny.Printers.Network;


/// <summary>Options for <see cref="INetworkPrinterScanner.Sweep"/>.</summary>
public sealed record NetworkPrinterSweepConfig
{
    /// <summary>The TCP ports to probe on every host. Defaults to 9100 (RAW / JetDirect).</summary>
    public IReadOnlyList<int> Ports { get; init; } = [9100];

    /// <summary>
    /// The IPv4 networks to sweep. Null (the default) sweeps the subnet of every active IPv4 interface on this
    /// device, narrowed to the surrounding /24 when the subnet is wider, and skips this device's own address.
    /// </summary>
    public IReadOnlyList<IPNetwork>? Networks { get; init; }

    /// <summary>How long to wait for each connect. A printer on the LAN answers in milliseconds. Default 500ms.</summary>
    public TimeSpan ProbeTimeout { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>How many connects run at once. Default 64.</summary>
    public int MaxConcurrency { get; init; } = 64;

    /// <summary>The capabilities given to every printer found. Defaults to 80mm.</summary>
    public PrinterCapabilities Capabilities { get; init; } = PrinterCapabilities.Paper80mm;
}

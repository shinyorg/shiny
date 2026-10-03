using System.Collections.Generic;

namespace Shiny.Printers.Network;


/// <summary>
/// A network printer surfaced by <see cref="INetworkPrinterScanner"/> via mDNS. Carries the resolved
/// <see cref="Host"/> / <see cref="Port"/> to connect with and the advertised TXT <see cref="Properties"/>.
/// </summary>
public sealed record DiscoveredNetworkPrinter
{
    /// <summary>The advertised service instance name (e.g. "Front Counter").</summary>
    public required string Name { get; init; }

    /// <summary>The resolved IP address (preferred) or host name to connect to.</summary>
    public required string Host { get; init; }

    /// <summary>The advertised port (9100 for <c>_pdl-datastream._tcp</c>).</summary>
    public required int Port { get; init; }

    /// <summary>The DNS-SD service type the printer was found under.</summary>
    public required string ServiceType { get; init; }

    /// <summary>The decoded TXT record (vendor, model, etc), when advertised.</summary>
    public IReadOnlyDictionary<string, string> Properties { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Best-guess capabilities for this printer. mDNS rarely advertises paper width, so this defaults to
    /// 80mm (the common networked receipt format); override before connecting if you know better.
    /// </summary>
    public PrinterCapabilities Capabilities { get; init; } = PrinterCapabilities.Paper80mm;
}

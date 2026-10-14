using System.Collections.Generic;

namespace Shiny.Printers.Network;


/// <summary>
/// A network printer surfaced by <see cref="INetworkPrinterScanner"/> via mDNS or a subnet sweep. Carries the
/// <see cref="Host"/> / <see cref="Port"/> to connect with and, for mDNS, the advertised TXT <see cref="Properties"/>.
/// </summary>
public sealed record DiscoveredNetworkPrinter
{
    /// <summary>The advertised service instance name (e.g. "Front Counter"). A swept printer has no name, so this is its IP address.</summary>
    public required string Name { get; init; }

    /// <summary>The resolved IP address (preferred) or host name to connect to.</summary>
    public required string Host { get; init; }

    /// <summary>The advertised port (9100 for <c>_pdl-datastream._tcp</c>).</summary>
    public required int Port { get; init; }

    /// <summary>The DNS-SD service type the printer was found under. Empty for a swept printer.</summary>
    public required string ServiceType { get; init; }

    /// <summary>Whether the printer advertised itself over mDNS or answered a subnet sweep.</summary>
    public NetworkPrinterDiscoveryMethod Method { get; init; } = NetworkPrinterDiscoveryMethod.Mdns;

    /// <summary>The decoded TXT record (vendor, model, etc), when advertised.</summary>
    public IReadOnlyDictionary<string, string> Properties { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Best-guess capabilities for this printer. mDNS rarely advertises paper width, so this defaults to
    /// 80mm (the common networked receipt format); override before connecting if you know better.
    /// </summary>
    public PrinterCapabilities Capabilities { get; init; } = PrinterCapabilities.Paper80mm;
}

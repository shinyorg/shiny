namespace Shiny.Printers.Network;


/// <summary>How a <see cref="DiscoveredNetworkPrinter"/> was found.</summary>
public enum NetworkPrinterDiscoveryMethod
{
    /// <summary>The printer advertised itself over mDNS / DNS-SD.</summary>
    Mdns,

    /// <summary>The printer accepted a TCP connect during a subnet sweep.</summary>
    PortSweep
}

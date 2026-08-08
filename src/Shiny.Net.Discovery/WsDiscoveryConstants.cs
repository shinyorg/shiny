using System.Xml;

namespace Shiny.Net.Discovery;


/// <summary>
/// Which WS-Discovery specification version a message uses. The two are wire-incompatible - they
/// differ in the discovery namespace, the WS-Addressing namespace, the multicast To URI and the
/// anonymous reply URI.
/// </summary>
[Flags]
public enum WsDiscoveryProfile
{
    None = 0,

    /// <summary>
    /// The 2005/04 draft. This is what ONVIF cameras and Windows' own WSD stack speak, and many
    /// devices answer nothing else.
    /// </summary>
    Ws2005 = 1,

    /// <summary>
    /// The OASIS WS-Discovery 1.1 standard from 2009/01.
    /// </summary>
    Ws2009 = 2,

    /// <summary>
    /// Both, sent as two separate datagrams. This is the default and what you want unless you
    /// know every device on your network.
    /// </summary>
    All = Ws2005 | Ws2009
}


/// <summary>
/// Well known WS-Discovery values.
/// </summary>
public static class WsDiscoveryConstants
{
    /// <summary>
    /// The IPv4 multicast group. Shared with SSDP, which uses a different port on the same group.
    /// </summary>
    public const string IPv4MulticastAddress = "239.255.255.250";

    /// <summary>
    /// The link-local IPv6 multicast group.
    /// </summary>
    public const string IPv6MulticastAddress = "ff02::c";

    /// <summary>
    /// The port WS-Discovery operates on.
    /// </summary>
    public const int Port = 3702;

    /// <summary>
    /// WS-Discovery is meant to stay on the local link, so the default hop limit is 1.
    /// </summary>
    public const int DefaultTtl = 1;

    /// <summary>
    /// The type ONVIF network video transmitters (cameras) answer to. This is the 2005-era type
    /// and remains the most widely supported way to find a camera.
    /// </summary>
    public static readonly XmlQualifiedName OnvifNetworkVideoTransmitter =
        new("NetworkVideoTransmitter", "http://www.onvif.org/ver10/network/wsdl");

    /// <summary>
    /// The newer ONVIF device type. Devices supporting it are expected to also answer
    /// <see cref="OnvifNetworkVideoTransmitter"/>, but not all of them do.
    /// </summary>
    public static readonly XmlQualifiedName OnvifDevice =
        new("Device", "http://www.onvif.org/ver10/device/wsdl");

    /// <summary>
    /// The type Windows machines and WSD printers/scanners publish.
    /// </summary>
    public static readonly XmlQualifiedName WsdpDevice =
        new("Device", "http://schemas.xmlsoap.org/ws/2006/02/devprof");
}

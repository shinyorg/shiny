using System.Net;
using System.Xml;

namespace Shiny.Net.Discovery;


/// <summary>
/// A WS-Discovery target service seen on the local network - an ONVIF camera, a WSD printer or
/// scanner, a Windows machine.
/// </summary>
public sealed record WsdTarget
{
    /// <summary>
    /// The endpoint reference address, normally "urn:uuid:...". This is the device's identity -
    /// key your collections on it.
    /// </summary>
    public required string EndpointReference { get; init; }

    /// <summary>
    /// The types the device implements, eg
    /// <c>{http://www.onvif.org/ver10/network/wsdl}NetworkVideoTransmitter</c>.
    /// </summary>
    public IReadOnlyList<XmlQualifiedName> Types { get; init; } = Array.Empty<XmlQualifiedName>();

    /// <summary>
    /// The scope URIs the device declares. ONVIF cameras publish their name, hardware, location
    /// and supported profiles here, eg "onvif://www.onvif.org/name/Front%20Door".
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Every transport address the device advertised. Frequently includes stale or unreachable
    /// entries after a DHCP change - prefer <see cref="PreferredAddress"/>.
    /// </summary>
    public IReadOnlyList<Uri> Addresses { get; init; } = Array.Empty<Uri>();

    /// <summary>
    /// The address most likely to actually work: one whose host matches where the message came
    /// from, else one on the same network, else the first advertised.
    /// </summary>
    public Uri? PreferredAddress { get; init; }

    /// <summary>
    /// Increments when the device's types, scopes or addresses change. A higher value than the
    /// one you hold means the metadata is stale.
    /// </summary>
    public uint MetadataVersion { get; init; }

    /// <summary>
    /// The address the message came from.
    /// </summary>
    public IPAddress? SourceAddress { get; init; }

    /// <summary>
    /// The index of the network interface the device was seen on, or 0 when unknown.
    /// </summary>
    public int InterfaceIndex { get; init; }

    /// <summary>
    /// Which specification version the device answered in. ONVIF cameras are usually
    /// <see cref="WsDiscoveryProfile.Ws2005"/> only.
    /// </summary>
    public WsDiscoveryProfile Profile { get; init; }

    /// <summary>
    /// True when the device declares the ONVIF network video transmitter type.
    /// </summary>
    public bool IsOnvifCamera
        => this.Types.Contains(WsDiscoveryConstants.OnvifNetworkVideoTransmitter);

    /// <summary>
    /// Reads a value out of an ONVIF-style scope, eg <c>GetScopeValue("name")</c> for
    /// "onvif://www.onvif.org/name/Front%20Door" returns "Front Door".
    /// </summary>
    /// <param name="category">The scope path segment to look under, eg "name" or "hardware".</param>
    public string? GetScopeValue(string category)
    {
        var prefix = $"/{category}/";

        return this.Scopes
            .Select(x => Uri.TryCreate(x, UriKind.Absolute, out var uri) ? uri.AbsolutePath : null)
            .Where(x => x != null && x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && x.Length > prefix.Length)
            .Select(x => Uri.UnescapeDataString(x![prefix.Length..]))
            .FirstOrDefault();
    }

    public override string ToString()
        => $"{this.EndpointReference} => {this.PreferredAddress?.AbsoluteUri ?? "(no address)"} [{String.Join(", ", this.Types)}]";
}


/// <summary>
/// Why a WS-Discovery result was emitted.
/// </summary>
public enum WsDiscoveryStatus
{
    /// <summary>
    /// The target appeared or its metadata changed. Treat as an upsert keyed on
    /// <see cref="WsdTarget.EndpointReference"/>.
    /// </summary>
    Found,

    /// <summary>
    /// The target sent a Bye. Only <see cref="WsdTarget.EndpointReference"/> is guaranteed.
    /// </summary>
    Lost
}


/// <summary>
/// A single change emitted by
/// <see cref="IWsDiscoveryManager.Browse(WsdProbeConfig, CancellationToken)"/>.
/// </summary>
/// <param name="Status">Whether the target appeared or went away.</param>
/// <param name="Target">The target the change applies to.</param>
public sealed record WsDiscoveryResult(WsDiscoveryStatus Status, WsdTarget Target);

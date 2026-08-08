using System.Net;

namespace Shiny.Net.Discovery;


/// <summary>
/// A UPnP device seen on the local network over SSDP.
/// </summary>
/// <remarks>
/// This is one device, not one advertisement. A device announces itself several times over - once
/// as a root device, once per UDN, once per device type and once per service type - and all of
/// those collapse into a single instance here, keyed on <see cref="Udn"/>, with the individual
/// announcements listed in <see cref="NotificationTypes"/>.
/// </remarks>
public sealed record SsdpDevice
{
    /// <summary>
    /// The unique device name, eg "uuid:2f402f80-da50-11e1-9b23-0017880924f0". This is the
    /// device's identity - key your own collections on it rather than on the location or USN.
    /// </summary>
    public required string Udn { get; init; }

    /// <summary>
    /// The URL of the device description document. Null when only a goodbye has been seen, since
    /// those carry no location.
    /// </summary>
    public Uri? Location { get; init; }

    /// <summary>
    /// The device's SERVER header, eg "Linux/4.4 UPnP/1.0 MyDevice/2.1". Free-form and often the
    /// only clue about what a device actually is before its description is fetched.
    /// </summary>
    public string? Server { get; init; }

    /// <summary>
    /// Every notification type this device has announced - "upnp:rootdevice", its UDN, and its
    /// device and service type URNs.
    /// </summary>
    public IReadOnlyList<string> NotificationTypes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// The address the advertisement came from. Also the address this library will allow the
    /// description to be fetched from.
    /// </summary>
    public IPAddress? SourceAddress { get; init; }

    /// <summary>
    /// The index of the network interface the device was seen on, or 0 when unknown.
    /// </summary>
    public int InterfaceIndex { get; init; }

    /// <summary>
    /// The device's UPnP 1.1 boot id. It increases every time the device restarts or changes
    /// network, and is absent on UPnP 1.0 devices.
    /// </summary>
    public uint? BootId { get; init; }

    /// <summary>
    /// The device's UPnP 1.1 config id. A change means the description document changed and any
    /// cached copy should be re-fetched.
    /// </summary>
    public uint? ConfigId { get; init; }

    /// <summary>
    /// The port for unicast searches when the device does not listen on 1900.
    /// </summary>
    public int? SearchPort { get; init; }

    /// <summary>
    /// Every header from the most recent advertisement, case-insensitive. Vendors put a lot of
    /// useful non-standard information here.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } = SsdpConstants.EmptyHeaders;

    /// <summary>
    /// True when the device announced itself as a UPnP root device rather than an embedded one.
    /// </summary>
    public bool IsRootDevice
        => this.NotificationTypes.Any(x => x.Equals(SsdpConstants.RootDevice, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The first device type URN announced, eg "urn:schemas-upnp-org:device:MediaServer:1".
    /// </summary>
    public string? DeviceType
        => this.NotificationTypes.FirstOrDefault(x => Managed.Ssdp.UsnParser.TryParseTypeUrn(x, out var urn) && urn.IsDevice);

    /// <summary>
    /// Every service type URN announced, eg "urn:schemas-upnp-org:service:ContentDirectory:1".
    /// </summary>
    public IReadOnlyList<string> ServiceTypes
        => this.NotificationTypes
            .Where(x => Managed.Ssdp.UsnParser.TryParseTypeUrn(x, out var urn) && urn.IsService)
            .ToList();

    /// <summary>
    /// Reads a header, returning null when it is absent.
    /// </summary>
    public string? GetHeader(string name)
        => this.Headers.TryGetValue(name, out var value) ? value : null;

    public override string ToString()
        => $"{this.Udn} => {this.Location?.AbsoluteUri ?? "(no location)"} [{String.Join(", ", this.NotificationTypes)}]";
}

namespace Shiny.Net.Discovery;


/// <summary>
/// A UPnP device description document, fetched from a device's advertised LOCATION.
/// </summary>
/// <remarks>
/// This is discovery metadata only. Invoking service actions (SOAP control) and subscribing to
/// state changes (GENA eventing) are out of scope - <see cref="UpnpService.ControlUrl"/> and
/// <see cref="UpnpService.EventSubscriptionUrl"/> are surfaced so you can drive your own client.
/// </remarks>
public sealed record UpnpDeviceDescription
{
    /// <summary>
    /// The unique device name, eg "uuid:2f402f80-da50-11e1-9b23-0017880924f0".
    /// </summary>
    public required string Udn { get; init; }

    /// <summary>
    /// The device type URN, eg "urn:schemas-upnp-org:device:MediaServer:1".
    /// </summary>
    public string? DeviceType { get; init; }

    /// <summary>
    /// The short name meant to be shown to a person, eg "Living Room Speaker".
    /// </summary>
    public string? FriendlyName { get; init; }

    public string? Manufacturer { get; init; }

    public Uri? ManufacturerUrl { get; init; }

    public string? ModelName { get; init; }

    public string? ModelNumber { get; init; }

    public string? ModelDescription { get; init; }

    public Uri? ModelUrl { get; init; }

    public string? SerialNumber { get; init; }

    /// <summary>
    /// The Universal Product Code, when the vendor bothered to set one.
    /// </summary>
    public string? Upc { get; init; }

    /// <summary>
    /// A page to show the user, if the device serves one.
    /// </summary>
    public Uri? PresentationUrl { get; init; }

    /// <summary>
    /// Icons the device offers, with URLs already resolved to absolute.
    /// </summary>
    public IReadOnlyList<UpnpIcon> Icons { get; init; } = Array.Empty<UpnpIcon>();

    /// <summary>
    /// The services this device exposes.
    /// </summary>
    public IReadOnlyList<UpnpService> Services { get; init; } = Array.Empty<UpnpService>();

    /// <summary>
    /// Devices embedded inside this one. They share the same description document and each has
    /// its own UDN.
    /// </summary>
    public IReadOnlyList<UpnpDeviceDescription> Devices { get; init; } = Array.Empty<UpnpDeviceDescription>();

    /// <summary>
    /// Walks this device and everything embedded beneath it.
    /// </summary>
    public IEnumerable<UpnpDeviceDescription> Flatten()
    {
        yield return this;

        foreach (var child in this.Devices)
        {
            foreach (var descendant in child.Flatten())
                yield return descendant;
        }
    }

    public override string ToString()
        => $"{this.FriendlyName ?? this.Udn} ({this.ModelName ?? this.DeviceType}) - {this.Services.Count} service(s)";
}


/// <summary>
/// An icon a device offers for display.
/// </summary>
public sealed record UpnpIcon
{
    /// <summary>The MIME type, eg "image/png".</summary>
    public string? MimeType { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    /// <summary>Colour depth in bits per pixel.</summary>
    public int Depth { get; init; }

    /// <summary>The absolute URL to fetch it from.</summary>
    public Uri? Url { get; init; }

    public override string ToString() => $"{this.MimeType} {this.Width}x{this.Height} => {this.Url}";
}


/// <summary>
/// A service exposed by a UPnP device.
/// </summary>
public sealed record UpnpService
{
    /// <summary>
    /// The service type URN, eg "urn:schemas-upnp-org:service:ContentDirectory:1".
    /// </summary>
    public string? ServiceType { get; init; }

    /// <summary>
    /// The service id, eg "urn:upnp-org:serviceId:ContentDirectory". Note the "upnp-org" prefix,
    /// which differs from the "schemas-upnp-org" used by <see cref="ServiceType"/>.
    /// </summary>
    public string? ServiceId { get; init; }

    /// <summary>
    /// Where the service control protocol description (SCPD) document lives.
    /// </summary>
    public Uri? ScpdUrl { get; init; }

    /// <summary>
    /// Where SOAP actions would be posted. This library does not invoke them.
    /// </summary>
    public Uri? ControlUrl { get; init; }

    /// <summary>
    /// Where GENA subscriptions would be sent. This library does not subscribe. Present but
    /// empty in the document when the service has no eventing.
    /// </summary>
    public Uri? EventSubscriptionUrl { get; init; }

    public override string ToString() => this.ServiceType ?? this.ServiceId ?? "(unknown service)";
}

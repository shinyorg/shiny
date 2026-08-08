using System.Diagnostics.CodeAnalysis;

namespace Shiny.Net.Discovery;


/// <summary>
/// What to advertise over SSDP. You are responsible for serving the description document at
/// <see cref="Location"/> - this only announces that it exists.
/// </summary>
public sealed record SsdpDeviceRegistration
{
    public SsdpDeviceRegistration() { }

    /// <param name="udn">The device's unique name. A bare GUID is accepted and prefixed.</param>
    /// <param name="location">The URL your device description document is served from.</param>
    [SetsRequiredMembers]
    public SsdpDeviceRegistration(string udn, Uri location)
    {
        this.Udn = udn;
        this.Location = location;
    }

    /// <summary>
    /// The unique device name, normally "uuid:" followed by a GUID. Keep it stable across
    /// restarts - control points use it as the device's identity.
    /// </summary>
    public required string Udn { get; init; }

    /// <summary>
    /// The absolute URL of the device description document.
    /// </summary>
    public required Uri Location { get; init; }

    /// <summary>
    /// The device type URN to advertise, eg "urn:schemas-upnp-org:device:MediaServer:1".
    /// </summary>
    public string? DeviceType { get; init; }

    /// <summary>
    /// Service type URNs to advertise, eg "urn:schemas-upnp-org:service:ContentDirectory:1".
    /// </summary>
    public IReadOnlyList<string> ServiceTypes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// When true (the default) the device also advertises itself as "upnp:rootdevice". Set false
    /// for an embedded device.
    /// </summary>
    public bool IsRootDevice { get; init; } = true;

    /// <summary>
    /// How long control points may cache the advertisement. The spec requires at least 30
    /// minutes; the advertisement is refreshed at a random point under half of it.
    /// </summary>
    public TimeSpan MaxAge { get; init; } = SsdpConstants.DefaultMaxAge;

    /// <summary>
    /// Overrides the SERVER header. Defaults to an OS/UPnP/Shiny product token.
    /// </summary>
    public string? Server { get; init; }

    /// <summary>
    /// The CONFIGID.UPNP.ORG value. Increment it whenever the description document changes so
    /// control points know to re-fetch it.
    /// </summary>
    public uint ConfigId { get; init; } = 1;
}

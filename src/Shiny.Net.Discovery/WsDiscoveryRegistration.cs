using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace Shiny.Net.Discovery;


/// <summary>
/// What to advertise over WS-Discovery. You are responsible for actually serving something at
/// <see cref="Addresses"/> - this only announces that it exists.
/// </summary>
public sealed record WsDiscoveryRegistration
{
    public WsDiscoveryRegistration() { }

    /// <param name="endpointReference">
    /// This service's stable identity, normally "urn:uuid:" followed by a GUID.
    /// </param>
    [SetsRequiredMembers]
    public WsDiscoveryRegistration(string endpointReference)
        => this.EndpointReference = endpointReference;

    /// <summary>
    /// The endpoint reference address. Keep it stable across restarts - clients use it as the
    /// service's identity. A bare GUID is accepted and prefixed.
    /// </summary>
    public required string EndpointReference { get; init; }

    /// <summary>
    /// The types this service implements. Clients probing for any of these will match.
    /// </summary>
    public IReadOnlyList<XmlQualifiedName> Types { get; init; } = Array.Empty<XmlQualifiedName>();

    /// <summary>
    /// Scope URIs describing this service, eg "onvif://www.onvif.org/name/Front%20Door".
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// The transport addresses clients should connect to.
    /// </summary>
    public IReadOnlyList<Uri> Addresses { get; init; } = Array.Empty<Uri>();

    /// <summary>
    /// The starting metadata version. It is incremented automatically when the host's addresses
    /// change; bump the initial value yourself if the service's types or scopes changed between
    /// runs.
    /// </summary>
    public uint MetadataVersion { get; init; } = 1;

    /// <summary>
    /// Which specification versions to advertise in. Both by default, so 2005-only clients (most
    /// ONVIF software, Windows) and 2009 clients can both see it.
    /// </summary>
    public WsDiscoveryProfile Profiles { get; init; } = WsDiscoveryProfile.All;
}

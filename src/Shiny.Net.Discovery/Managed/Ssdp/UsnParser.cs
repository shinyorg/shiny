namespace Shiny.Net.Discovery.Managed.Ssdp;


/// <summary>
/// A USN split into the device it identifies and the thing being advertised.
/// </summary>
/// <param name="Udn">The normalised device UDN, eg "uuid:2f402f80-da50-11e1-9b23-0017880924f0".</param>
/// <param name="NotificationType">
/// The advertised type, eg "upnp:rootdevice" or "urn:schemas-upnp-org:device:MediaServer:1".
/// Null when the USN is a bare UDN, which is itself a valid advertisement.
/// </param>
readonly record struct UsnParts(string Udn, string? NotificationType);


/// <summary>
/// A parsed UPnP type URN, eg "urn:schemas-upnp-org:device:MediaServer:1".
/// </summary>
/// <param name="Domain">"schemas-upnp-org", or a vendor domain with dots replaced by hyphens.</param>
/// <param name="Kind">"device" or "service".</param>
/// <param name="Type">The type name, eg "MediaServer".</param>
/// <param name="Version">The trailing integer version.</param>
readonly record struct UpnpTypeUrn(string Domain, string Kind, string Type, int Version)
{
    public bool IsDevice => this.Kind.Equals("device", StringComparison.OrdinalIgnoreCase);

    public bool IsService => this.Kind.Equals("service", StringComparison.OrdinalIgnoreCase);

    /// <summary>The URN without its version, which is what identity comparisons run on.</summary>
    public string BaseUrn => $"urn:{this.Domain}:{this.Kind}:{this.Type}";
}


/// <summary>
/// Parses the USN, NT and ST values that carry UPnP identity.
/// </summary>
static class UsnParser
{
    /// <summary>
    /// Splits a USN into its UDN and notification type. Returns false when there is no usable
    /// UDN, which is the only thing that makes a USN worthless.
    /// </summary>
    public static bool TryParse(string? usn, out UsnParts parts)
    {
        parts = default;

        if (String.IsNullOrWhiteSpace(usn))
            return false;

        var trimmed = usn.Trim();

        // split on the FIRST "::" - a vendor type after it can legitimately contain colons
        var separator = trimmed.IndexOf("::", StringComparison.Ordinal);

        var rawUdn = separator < 0 ? trimmed : trimmed[..separator];
        var type = separator < 0 ? null : trimmed[(separator + 2)..].Trim();

        var udn = NormalizeUdn(rawUdn);
        if (udn.Length == 0)
            return false;

        parts = new UsnParts(udn, String.IsNullOrWhiteSpace(type) ? null : type);
        return true;
    }


    /// <summary>
    /// Canonicalises a UDN so the same device seen through different advertisements keys the same.
    /// </summary>
    /// <remarks>
    /// The spec says "uuid:" followed by lowercase hex. Real devices send uppercase, wrap the GUID
    /// in braces, or leave the prefix off entirely, and all three appear on the same network.
    /// Casing is preserved for display; comparisons are ordinal-ignore-case.
    /// </remarks>
    public static string NormalizeUdn(string? raw)
    {
        if (String.IsNullOrWhiteSpace(raw))
            return String.Empty;

        var value = raw.Trim();

        if (value.StartsWith("uuid:", StringComparison.OrdinalIgnoreCase))
            value = value[5..].Trim();

        value = value.Trim('{', '}').Trim();

        return value.Length == 0 ? String.Empty : $"uuid:{value}";
    }


    /// <summary>
    /// Parses a UPnP device or service type URN. Returns false for anything else, including
    /// "upnp:rootdevice" and bare UDNs.
    /// </summary>
    public static bool TryParseTypeUrn(string? value, out UpnpTypeUrn urn)
    {
        urn = default;

        if (String.IsNullOrWhiteSpace(value))
            return false;

        var segments = value.Trim().Split(':');
        if (segments.Length < 5 || !segments[0].Equals("urn", StringComparison.OrdinalIgnoreCase))
            return false;

        var kind = segments[2];
        if (!kind.Equals("device", StringComparison.OrdinalIgnoreCase) &&
            !kind.Equals("service", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // the version is always last; anything between the kind and it belongs to the type name
        if (!Int32.TryParse(segments[^1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var version))
            return false;

        var type = String.Join(':', segments[3..^1]);
        if (type.Length == 0)
            return false;

        urn = new UpnpTypeUrn(segments[1], kind.ToLowerInvariant(), type, version);
        return true;
    }
}

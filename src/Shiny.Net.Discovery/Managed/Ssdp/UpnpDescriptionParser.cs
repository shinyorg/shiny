using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Shiny.Net.Discovery.Managed.Ssdp;


/// <summary>
/// Parses a UPnP device description document.
/// </summary>
/// <remarks>
/// <para>Element lookup deliberately ignores XML namespaces and matches on local name only. The
/// correct namespace is <c>urn:schemas-upnp-org:device-1-0</c>, but shipped devices variously
/// declare no default namespace, declare the wrong one, or bolt vendor namespaces onto the
/// document. Enforcing it means failing to read devices every other UPnP client can read.</para>
/// <para><see cref="XDocument"/> rather than <see cref="XmlReader"/> because this runs once per
/// device rather than per datagram, and the tree walk is far easier to follow. Both are
/// trim/AOT-safe; <c>XmlSerializer</c> would not be.</para>
/// </remarks>
static class UpnpDescriptionParser
{
    /// <summary>Bounds a hostile or broken document. Real descriptions are a few KB.</summary>
    public const int MaxDocumentBytes = 512 * 1024;

    const int MaxDeviceDepth = 8;
    const int MaxServices = 256;
    const int MaxIcons = 64;
    const int MaxEmbeddedDevices = 128;


    /// <summary>
    /// Parses a description document.
    /// </summary>
    /// <param name="data">The raw bytes as fetched.</param>
    /// <param name="location">
    /// The URL it was fetched from, after redirects. Relative URLs inside the document resolve
    /// against this unless the document carries a usable URLBase.
    /// </param>
    /// <exception cref="SsdpException">The document is not a parseable UPnP description.</exception>
    public static UpnpDeviceDescription Parse(byte[] data, Uri location)
    {
        if (data.Length == 0)
            throw new SsdpException($"The device description at {location} was empty.");

        var document = Load(data, location);

        var root = document.Root
            ?? throw new SsdpException($"The device description at {location} has no root element.");

        var deviceElement = Child(root, "device")
            ?? throw new SsdpException($"The device description at {location} has no <device> element.");

        return ParseDevice(deviceElement, ResolveBase(root, location), 0);
    }


    static XDocument Load(byte[] data, Uri location)
    {
        try
        {
            return LoadCore(data);
        }
        catch (XmlException original)
        {
            // devices ship declarations like encoding="utf8" or "ISO 8859-1" that XmlReader
            // rejects outright; the bytes themselves are almost always UTF-8 or Latin-1
            var stripped = StripXmlDeclaration(data);
            if (stripped == null)
                throw new SsdpException($"The device description at {location} is not valid XML.", original);

            try
            {
                return LoadCore(stripped);
            }
            catch (XmlException ex)
            {
                throw new SsdpException($"The device description at {location} is not valid XML.", ex);
            }
        }
    }


    static XDocument LoadCore(byte[] data)
    {
        var settings = new XmlReaderSettings
        {
            // no external entities, no entity expansion, no network access from a document a
            // stranger on the network handed us
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = 0,
            MaxCharactersInDocument = MaxDocumentBytes,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true
        };

        // a stream rather than a string so a UTF-8 BOM is handled instead of throwing
        using var stream = new MemoryStream(data, false);
        using var reader = XmlReader.Create(stream, settings);
        return XDocument.Load(reader);
    }


    static byte[]? StripXmlDeclaration(byte[] data)
    {
        var head = Encoding.UTF8.GetString(data, 0, Math.Min(data.Length, 256));

        var start = head.IndexOf("<?xml", StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return null;

        var end = head.IndexOf("?>", start, StringComparison.Ordinal);
        if (end < 0)
            return null;

        var remainder = Encoding.UTF8.GetString(data)[(end + 2)..].TrimStart();
        return Encoding.UTF8.GetBytes(remainder);
    }


    /// <summary>
    /// Works out what relative URLs in the document resolve against.
    /// </summary>
    static Uri ResolveBase(XElement root, Uri location)
    {
        var raw = Value(root, "URLBase");
        if (raw == null || !Uri.TryCreate(raw, UriKind.Absolute, out var urlBase))
            return location;

        // URLBase is deprecated and routinely stale after a DHCP change - if it disagrees with
        // where we actually just fetched the document from, the fetch location is the truth
        if (!urlBase.Host.Equals(location.Host, StringComparison.OrdinalIgnoreCase))
            return location;

        // "http://192.168.1.5" without a trailing slash loses its last segment when resolved
        // against, which is the classic UPnP base URL bug
        if (!urlBase.AbsolutePath.EndsWith('/'))
            urlBase = new UriBuilder(urlBase) { Path = urlBase.AbsolutePath + "/" }.Uri;

        return urlBase;
    }


    static UpnpDeviceDescription ParseDevice(XElement element, Uri baseUri, int depth)
    {
        var services = Child(element, "serviceList")
            ?.Elements()
            .Where(x => x.Name.LocalName.Equals("service", StringComparison.OrdinalIgnoreCase))
            .Take(MaxServices)
            .Select(x => ParseService(x, baseUri))
            .ToList();

        var icons = Child(element, "iconList")
            ?.Elements()
            .Where(x => x.Name.LocalName.Equals("icon", StringComparison.OrdinalIgnoreCase))
            .Take(MaxIcons)
            .Select(x => ParseIcon(x, baseUri))
            .ToList();

        // depth is capped so a document that nests itself cannot exhaust the stack
        var embedded = depth >= MaxDeviceDepth
            ? null
            : Child(element, "deviceList")
                ?.Elements()
                .Where(x => x.Name.LocalName.Equals("device", StringComparison.OrdinalIgnoreCase))
                .Take(MaxEmbeddedDevices)
                .Select(x => ParseDevice(x, baseUri, depth + 1))
                .ToList();

        return new UpnpDeviceDescription
        {
            Udn = UsnParser.NormalizeUdn(Value(element, "UDN")),
            DeviceType = Value(element, "deviceType"),
            FriendlyName = Value(element, "friendlyName"),
            Manufacturer = Value(element, "manufacturer"),
            ManufacturerUrl = ResolveUrl(baseUri, Value(element, "manufacturerURL")),
            ModelName = Value(element, "modelName"),
            ModelNumber = Value(element, "modelNumber"),
            ModelDescription = Value(element, "modelDescription"),
            ModelUrl = ResolveUrl(baseUri, Value(element, "modelURL")),
            SerialNumber = Value(element, "serialNumber"),
            Upc = Value(element, "UPC"),
            PresentationUrl = ResolveUrl(baseUri, Value(element, "presentationURL")),
            Icons = icons ?? (IReadOnlyList<UpnpIcon>)Array.Empty<UpnpIcon>(),
            Services = services ?? (IReadOnlyList<UpnpService>)Array.Empty<UpnpService>(),
            Devices = embedded ?? (IReadOnlyList<UpnpDeviceDescription>)Array.Empty<UpnpDeviceDescription>()
        };
    }


    static UpnpService ParseService(XElement element, Uri baseUri) => new()
    {
        ServiceType = Value(element, "serviceType"),
        ServiceId = Value(element, "serviceId"),
        ScpdUrl = ResolveUrl(baseUri, Value(element, "SCPDURL")),
        ControlUrl = ResolveUrl(baseUri, Value(element, "controlURL")),
        EventSubscriptionUrl = ResolveUrl(baseUri, Value(element, "eventSubURL"))
    };


    static UpnpIcon ParseIcon(XElement element, Uri baseUri) => new()
    {
        MimeType = Value(element, "mimetype"),
        Width = ParseInt(Value(element, "width")),
        Height = ParseInt(Value(element, "height")),
        Depth = ParseInt(Value(element, "depth")),
        Url = ResolveUrl(baseUri, Value(element, "url"))
    };


    static int ParseInt(string? raw)
        => Int32.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;


    static Uri? ResolveUrl(Uri baseUri, string? raw)
        => String.IsNullOrWhiteSpace(raw) || !Uri.TryCreate(baseUri, raw.Trim(), out var resolved)
            ? null
            : resolved;


    static XElement? Child(XElement parent, string name)
        => parent.Elements().FirstOrDefault(x => x.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));


    static string? Value(XElement parent, string name)
    {
        var value = Child(parent, name)?.Value.Trim();
        return String.IsNullOrEmpty(value) ? null : value;
    }
}

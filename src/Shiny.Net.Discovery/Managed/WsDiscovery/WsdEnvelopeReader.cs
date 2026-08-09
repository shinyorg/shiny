using System.Xml;
using System.Xml.Linq;

namespace Shiny.Net.Discovery.Managed.WsDiscovery;


/// <summary>
/// Parses a SOAP-over-UDP WS-Discovery envelope.
/// </summary>
/// <remarks>
/// <para>This builds an <see cref="XDocument"/> rather than streaming with a bare
/// <see cref="XmlReader"/>, and that is deliberate. The <c>Types</c> element carries a
/// whitespace-separated list of <em>QNames</em> whose prefixes are declared somewhere up the
/// element chain - usually on the Envelope, occasionally on <c>Types</c> itself. Resolving those
/// needs the ancestor namespace scope, which a forward-only reader has already popped by the time
/// the text content has been read. Getting this wrong is the single most common WS-Discovery bug:
/// a naive string split silently fails to match every device on the network.</para>
/// <para>The document is still built through a hardened <see cref="XmlReader"/>, so no DTDs, no
/// entity expansion and no external resolution. Both APIs are trim/AOT-safe;
/// <c>XmlSerializer</c> would not be.</para>
/// </remarks>
static class WsdEnvelopeReader
{
    /// <summary>SOAP-over-UDP allows a large datagram, but nothing legitimate here is close.</summary>
    public const int MaxMessageSize = 64 * 1024;

    const int MaxTypes = 64;
    const int MaxScopes = 64;
    const int MaxXAddrs = 16;
    const int MaxMatches = 64;

    static readonly char[] listSeparators = [' ', '\t', '\r', '\n'];


    /// <summary>
    /// Parses a received datagram. Returns null for anything that is not a WS-Discovery envelope
    /// we understand - malformed XML, an unknown namespace, a missing action. Never throws.
    /// </summary>
    public static WsdMessage? TryParse(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0 || data.Length > MaxMessageSize)
            return null;

        XDocument document;
        try
        {
            document = Load(data.ToArray());
        }
        catch (Exception)
        {
            // unescaped ampersands and duplicate xmlns declarations are common enough on cheap
            // hardware that a parse failure is routine rather than exceptional
            return null;
        }

        var envelope = document.Root;
        if (envelope == null || !envelope.Name.LocalName.Equals("Envelope", StringComparison.Ordinal))
            return null;

        var header = Child(envelope, "Header");
        var body = Child(envelope, "Body");
        if (body == null)
            return null;

        var action = header == null ? null : Value(header, "Action");
        var bodyElement = body.Elements().FirstOrDefault();
        if (bodyElement == null)
            return null;

        // the action names the message, but plenty of devices omit or mangle it - the body
        // element's own name says the same thing and is the more reliable of the two
        var messageName = ExtractMessageName(action) ?? bodyElement.Name.LocalName;

        var profile = WsdVersionProfile.ForNamespace(bodyElement.Name.NamespaceName)
            ?? WsdVersionProfile.ForNamespace(ExtractNamespace(action));

        if (profile == null)
            return null;

        return new WsdMessage
        {
            Profile = profile,
            MessageName = messageName,
            MessageId = header == null ? null : Value(header, "MessageID"),
            RelatesTo = header == null ? null : Value(header, "RelatesTo"),
            AppSequence = header == null ? null : ParseAppSequence(header),
            Matches = ParseMatches(messageName, bodyElement),
            Probe = messageName.Equals(WsdMessageNames.Probe, StringComparison.Ordinal)
                ? ParseProbe(bodyElement)
                : null,
            ResolveTarget = messageName.Equals(WsdMessageNames.Resolve, StringComparison.Ordinal)
                ? ReadEndpointReference(bodyElement)
                : null
        };
    }


    static XDocument Load(byte[] data)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = 0,
            MaxCharactersInDocument = MaxMessageSize,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true
        };

        using var stream = new MemoryStream(data, false);
        using var reader = XmlReader.Create(stream, settings);
        return XDocument.Load(reader);
    }


    static IReadOnlyList<WsdMatch> ParseMatches(string messageName, XElement bodyElement)
    {
        // Hello and Bye carry the target inline; the Matches messages wrap one per child
        if (messageName.Equals(WsdMessageNames.Hello, StringComparison.Ordinal) ||
            messageName.Equals(WsdMessageNames.Bye, StringComparison.Ordinal))
        {
            var single = ParseMatch(bodyElement);
            return single == null ? Array.Empty<WsdMatch>() : new[] { single };
        }

        if (!messageName.Equals(WsdMessageNames.ProbeMatches, StringComparison.Ordinal) &&
            !messageName.Equals(WsdMessageNames.ResolveMatches, StringComparison.Ordinal))
        {
            return Array.Empty<WsdMatch>();
        }

        return bodyElement
            .Elements()
            .Take(MaxMatches)
            .Select(ParseMatch)
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();
    }


    static WsdMatch? ParseMatch(XElement element)
    {
        var epr = ReadEndpointReference(element);
        if (String.IsNullOrWhiteSpace(epr))
            return null;

        var typesElement = Child(element, "Types");
        var scopesElement = Child(element, "Scopes");

        return new WsdMatch(
            epr,
            typesElement == null ? Array.Empty<XmlQualifiedName>() : ParseQNames(typesElement),
            scopesElement == null ? Array.Empty<string>() : SplitList(scopesElement.Value, MaxScopes),
            SplitList(Value(element, "XAddrs"), MaxXAddrs),
            ParseUInt(Value(element, "MetadataVersion")) ?? 0
        );
    }


    static WsdProbeCriteria ParseProbe(XElement bodyElement)
    {
        var typesElement = Child(bodyElement, "Types");
        var scopesElement = Child(bodyElement, "Scopes");

        return new WsdProbeCriteria(
            typesElement == null ? Array.Empty<XmlQualifiedName>() : ParseQNames(typesElement),
            scopesElement == null ? Array.Empty<string>() : SplitList(scopesElement.Value, MaxScopes),
            // MatchBy is an unqualified attribute on Scopes
            scopesElement?.Attribute("MatchBy")?.Value?.Trim()
        );
    }


    /// <summary>
    /// Reads the address out of an EndpointReference, wherever the device chose to put it.
    /// </summary>
    static string? ReadEndpointReference(XElement element)
    {
        var epr = Child(element, "EndpointReference");
        var address = epr == null ? null : Value(epr, "Address");

        // some devices flatten the reference and put Address straight in the body
        address ??= Value(element, "Address");

        return String.IsNullOrWhiteSpace(address) ? null : NormalizeEndpointReference(address);
    }


    /// <summary>
    /// Canonicalises an endpoint reference so the same device keys the same across messages.
    /// Devices send braces around the GUID, omit the urn:uuid: prefix, or vary the casing.
    /// </summary>
    public static string NormalizeEndpointReference(string raw)
    {
        var value = raw.Trim();

        var body = value.StartsWith("urn:uuid:", StringComparison.OrdinalIgnoreCase)
            ? value[9..].Trim()
            : value;

        body = body.Trim('{', '}').Trim();

        // only rewrite things that really are UUIDs. An EPR is allowed to be any URI - a URL, or
        // a urn:uuid: whose body is not a GUID - and those must come back through untouched
        // rather than losing their scheme.
        return Guid.TryParse(body, out var uuid) ? $"urn:uuid:{uuid:D}" : value;
    }


    /// <summary>
    /// Parses a QName list, resolving each prefix against the namespace declarations in scope.
    /// </summary>
    static IReadOnlyList<XmlQualifiedName> ParseQNames(XElement element)
    {
        var results = new List<XmlQualifiedName>();

        foreach (var token in SplitList(element.Value, MaxTypes))
        {
            var colon = token.IndexOf(':');
            if (colon <= 0)
            {
                // an unprefixed QName in element content resolves against the default namespace
                results.Add(new XmlQualifiedName(token, element.GetDefaultNamespace().NamespaceName));
            }
            else
            {
                // GetNamespaceOfPrefix walks ancestors, which is exactly what the spec requires
                // and exactly what a forward-only reader cannot do after reading the text
                var ns = element.GetNamespaceOfPrefix(token[..colon]);
                results.Add(new XmlQualifiedName(token[(colon + 1)..], ns?.NamespaceName ?? String.Empty));
            }
        }
        return results;
    }


    static WsdAppSequence? ParseAppSequence(XElement header)
    {
        var element = Child(header, "AppSequence");
        if (element == null)
            return null;

        var instance = ParseUInt(element.Attribute("InstanceId")?.Value);
        if (instance == null)
            return null;

        return new WsdAppSequence(
            instance.Value,
            element.Attribute("SequenceId")?.Value,
            ParseUInt(element.Attribute("MessageNumber")?.Value) ?? 0
        );
    }


    static string? ExtractMessageName(string? action)
    {
        if (String.IsNullOrWhiteSpace(action))
            return null;

        var slash = action.TrimEnd('/').LastIndexOf('/');
        return slash < 0 ? null : action.TrimEnd('/')[(slash + 1)..];
    }


    static string? ExtractNamespace(string? action)
    {
        if (String.IsNullOrWhiteSpace(action))
            return null;

        var slash = action.TrimEnd('/').LastIndexOf('/');
        return slash < 0 ? null : action.TrimEnd('/')[..slash];
    }


    static IReadOnlyList<string> SplitList(string? raw, int max)
        => String.IsNullOrWhiteSpace(raw)
            ? Array.Empty<string>()
            : raw.Split(listSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(max)
                .ToList();


    static uint? ParseUInt(string? raw)
        => UInt32.TryParse(raw?.Trim(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : null;


    static XElement? Child(XElement parent, string name)
        => parent.Elements().FirstOrDefault(x => x.Name.LocalName.Equals(name, StringComparison.Ordinal));


    static string? Value(XElement parent, string name)
    {
        var value = Child(parent, name)?.Value.Trim();
        return String.IsNullOrEmpty(value) ? null : value;
    }
}

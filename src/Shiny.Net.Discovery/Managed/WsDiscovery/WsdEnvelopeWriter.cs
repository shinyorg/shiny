using System.Globalization;
using System.Text;
using System.Xml;

namespace Shiny.Net.Discovery.Managed.WsDiscovery;


/// <summary>
/// Builds WS-Discovery SOAP-over-UDP envelopes.
/// </summary>
/// <remarks>
/// Hand-built rather than written through <c>XmlWriter</c> because the envelope shape is fixed and
/// some devices - ONVIF cameras especially - are fussy about exact formatting. This keeps the
/// output compact, deterministic and easy to compare against a packet capture. Every namespace is
/// declared on the Envelope, except the ad-hoc prefixes minted for QNames in a Types list.
/// </remarks>
static class WsdEnvelopeWriter
{
    public static byte[] Probe(
        WsdVersionProfile profile,
        string messageId,
        IReadOnlyList<XmlQualifiedName> types,
        IReadOnlyList<string> scopes,
        string? matchBy
    )
    {
        var body = new StringBuilder();
        body.Append("<d:Probe>");
        AppendTypes(body, types);
        AppendScopes(body, scopes, matchBy);
        body.Append("</d:Probe>");

        return Build(profile, WsdMessageNames.Probe, messageId, profile.ToUri, null, null, body.ToString());
    }


    public static byte[] Resolve(WsdVersionProfile profile, string messageId, string endpointReference)
    {
        var body = new StringBuilder()
            .Append("<d:Resolve>")
            .Append(EndpointReference(endpointReference))
            .Append("</d:Resolve>");

        return Build(profile, WsdMessageNames.Resolve, messageId, profile.ToUri, null, null, body.ToString());
    }


    public static byte[] Hello(WsdVersionProfile profile, string messageId, WsdAppSequence sequence, WsdMatch match)
        => Announcement(profile, WsdMessageNames.Hello, messageId, sequence, match);


    public static byte[] Bye(WsdVersionProfile profile, string messageId, WsdAppSequence sequence, WsdMatch match)
        => Announcement(profile, WsdMessageNames.Bye, messageId, sequence, match);


    static byte[] Announcement(
        WsdVersionProfile profile,
        string messageName,
        string messageId,
        WsdAppSequence sequence,
        WsdMatch match
    )
    {
        var body = new StringBuilder();
        body.Append("<d:").Append(messageName).Append('>');
        AppendMatchContent(body, match);
        body.Append("</d:").Append(messageName).Append('>');

        return Build(profile, messageName, messageId, profile.ToUri, null, sequence, body.ToString());
    }


    /// <summary>
    /// Builds a ProbeMatches or ResolveMatches. These go back unicast to whoever asked, so the To
    /// header is the anonymous URI rather than the multicast one.
    /// </summary>
    public static byte[] Matches(
        WsdVersionProfile profile,
        string messageName,
        string messageId,
        string relatesTo,
        WsdAppSequence sequence,
        IReadOnlyList<WsdMatch> matches
    )
    {
        var itemName = messageName.Equals(WsdMessageNames.ProbeMatches, StringComparison.Ordinal)
            ? "ProbeMatch"
            : "ResolveMatch";

        var body = new StringBuilder();
        body.Append("<d:").Append(messageName).Append('>');

        foreach (var match in matches)
        {
            body.Append("<d:").Append(itemName).Append('>');
            AppendMatchContent(body, match);
            body.Append("</d:").Append(itemName).Append('>');
        }
        body.Append("</d:").Append(messageName).Append('>');

        return Build(profile, messageName, messageId, profile.AnonymousUri, relatesTo, sequence, body.ToString());
    }


    static void AppendMatchContent(StringBuilder body, WsdMatch match)
    {
        body.Append(EndpointReference(match.EndpointReference));
        AppendTypes(body, match.Types);
        AppendScopes(body, match.Scopes, null);

        if (match.XAddrs.Count > 0)
            body.Append("<d:XAddrs>").Append(Escape(String.Join(' ', match.XAddrs))).Append("</d:XAddrs>");

        body
            .Append("<d:MetadataVersion>")
            .Append(match.MetadataVersion.ToString(CultureInfo.InvariantCulture))
            .Append("</d:MetadataVersion>");
    }


    /// <summary>
    /// Writes a QName list, minting a prefix per distinct namespace on the element itself. That
    /// placement is legal and is what a correct reader resolves against.
    /// </summary>
    static void AppendTypes(StringBuilder body, IReadOnlyList<XmlQualifiedName> types)
    {
        if (types.Count == 0)
            return;

        var namespaces = types
            .Select(x => x.Namespace)
            .Where(x => !String.IsNullOrEmpty(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        body.Append("<d:Types");
        for (var i = 0; i < namespaces.Count; i++)
            body.Append(" xmlns:t").Append(i).Append("=\"").Append(Escape(namespaces[i])).Append('"');

        body.Append('>');

        for (var i = 0; i < types.Count; i++)
        {
            if (i > 0)
                body.Append(' ');

            var index = namespaces.IndexOf(types[i].Namespace);
            if (index >= 0)
                body.Append('t').Append(index).Append(':');

            body.Append(Escape(types[i].Name));
        }
        body.Append("</d:Types>");
    }


    static void AppendScopes(StringBuilder body, IReadOnlyList<string> scopes, string? matchBy)
    {
        if (scopes.Count == 0)
            return;

        body.Append("<d:Scopes");
        if (!String.IsNullOrWhiteSpace(matchBy))
            body.Append(" MatchBy=\"").Append(Escape(matchBy)).Append('"');

        body.Append('>').Append(Escape(String.Join(' ', scopes))).Append("</d:Scopes>");
    }


    static string EndpointReference(string address)
        => $"<a:EndpointReference><a:Address>{Escape(address)}</a:Address></a:EndpointReference>";


    static byte[] Build(
        WsdVersionProfile profile,
        string messageName,
        string messageId,
        string to,
        string? relatesTo,
        WsdAppSequence? sequence,
        string body
    )
    {
        var envelope = new StringBuilder()
            .Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>")
            .Append("<s:Envelope")
            .Append(" xmlns:s=\"").Append(WsdVersionProfile.SoapNamespace).Append('"')
            .Append(" xmlns:a=\"").Append(profile.AddressingNamespace).Append('"')
            .Append(" xmlns:d=\"").Append(profile.DiscoveryNamespace).Append("\">")
            .Append("<s:Header>")
            // mustUnderstand="1" rather than "true" - both are legal SOAP 1.2 but some cameras
            // only accept the numeric form
            .Append("<a:Action s:mustUnderstand=\"1\">").Append(Escape(profile.ActionFor(messageName))).Append("</a:Action>")
            .Append("<a:MessageID>").Append(Escape(messageId)).Append("</a:MessageID>")
            .Append("<a:To s:mustUnderstand=\"1\">").Append(Escape(to)).Append("</a:To>");

        if (!String.IsNullOrWhiteSpace(relatesTo))
            envelope.Append("<a:RelatesTo>").Append(Escape(relatesTo)).Append("</a:RelatesTo>");

        if (sequence != null)
        {
            envelope
                .Append("<d:AppSequence InstanceId=\"")
                .Append(sequence.Value.InstanceId.ToString(CultureInfo.InvariantCulture))
                .Append("\" MessageNumber=\"")
                .Append(sequence.Value.MessageNumber.ToString(CultureInfo.InvariantCulture))
                .Append("\"");

            if (!String.IsNullOrWhiteSpace(sequence.Value.SequenceId))
                envelope.Append(" SequenceId=\"").Append(Escape(sequence.Value.SequenceId)).Append('"');

            envelope.Append("/>");
        }

        envelope
            .Append("</s:Header>")
            .Append("<s:Body>")
            .Append(body)
            .Append("</s:Body>")
            .Append("</s:Envelope>");

        return Encoding.UTF8.GetBytes(envelope.ToString());
    }


    static string Escape(string value)
        => value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
}

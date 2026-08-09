using System.Xml;

namespace Shiny.Net.Discovery.Managed.WsDiscovery;


/// <summary>
/// Implements the WS-Discovery Types and Scopes matching rules.
/// </summary>
static class WsdScopeMatcher
{
    static readonly char[] pathSeparator = ['/'];


    /// <summary>
    /// A target matches when it implements every type the probe asked for. An empty probe list
    /// matches everything.
    /// </summary>
    public static bool MatchesTypes(
        IReadOnlyList<XmlQualifiedName> probeTypes,
        IReadOnlyList<XmlQualifiedName> targetTypes
    )
        => probeTypes.Count == 0 || probeTypes.All(targetTypes.Contains);


    /// <summary>
    /// A target matches when its scopes cover every scope the probe asked for, under the
    /// requested rule. An empty probe list matches everything.
    /// </summary>
    public static bool MatchesScopes(
        IReadOnlyList<string> probeScopes,
        IReadOnlyList<string> targetScopes,
        string? matchBy,
        WsdVersionProfile profile
    )
    {
        if (probeScopes.Count == 0)
            return true;

        var rule = ExtractRule(matchBy ?? profile.DefaultMatchBy);

        // an unknown rule must not match, per spec - guessing would produce false positives
        if (rule == null)
            return false;

        return probeScopes.All(wanted => targetScopes.Any(have => MatchesScope(wanted, have, rule)));
    }


    static bool MatchesScope(string probe, string target, string rule) => rule switch
    {
        "strcmp0" => String.Equals(probe, target, StringComparison.Ordinal),
        "uuid" => MatchesUuid(probe, target),
        "rfc2396" or "rfc3986" => MatchesUri(probe, target),
        // RFC 2255 LDAP URL matching. Nothing in the wild uses it and implementing it wrong is
        // worse than declining, so this deliberately never matches.
        _ => false
    };


    static bool MatchesUuid(string probe, string target)
    {
        var left = WsdEnvelopeReader.NormalizeEndpointReference(probe);
        var right = WsdEnvelopeReader.NormalizeEndpointReference(target);

        return String.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }


    /// <summary>
    /// The default rule: scheme and authority compared case-insensitively, path compared
    /// segment by segment and case-sensitively, with the probe's path a prefix of the target's.
    /// Query and fragment are ignored.
    /// </summary>
    /// <remarks>
    /// Segment-wise rather than string-wise is the part that matters:
    /// "onvif://www.onvif.org/type" matches ".../type/video_encoder" but "onvif://www.onvif.org/ty"
    /// must not match either of them.
    /// </remarks>
    static bool MatchesUri(string probe, string target)
    {
        if (!Uri.TryCreate(probe, UriKind.Absolute, out var wanted) ||
            !Uri.TryCreate(target, UriKind.Absolute, out var have))
        {
            // not URIs at all - fall back to exact comparison rather than silently matching
            return String.Equals(probe, target, StringComparison.Ordinal);
        }

        if (!String.Equals(wanted.Scheme, have.Scheme, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!String.Equals(wanted.Authority, have.Authority, StringComparison.OrdinalIgnoreCase))
            return false;

        var wantedSegments = wanted.AbsolutePath.Split(pathSeparator, StringSplitOptions.RemoveEmptyEntries);
        var haveSegments = have.AbsolutePath.Split(pathSeparator, StringSplitOptions.RemoveEmptyEntries);

        if (wantedSegments.Length > haveSegments.Length)
            return false;

        return !wantedSegments.Where((segment, i) => !String.Equals(segment, haveSegments[i], StringComparison.Ordinal)).Any();
    }


    /// <summary>
    /// Reduces a MatchBy URI to its rule name. The full URI differs between the 2005 and 2009
    /// namespaces but the trailing rule name does not.
    /// </summary>
    static string? ExtractRule(string? matchBy)
    {
        if (String.IsNullOrWhiteSpace(matchBy))
            return null;

        var value = matchBy.Trim().TrimEnd('/');
        var slash = value.LastIndexOf('/');
        var rule = slash < 0 ? value : value[(slash + 1)..];

        return rule.ToLowerInvariant() switch
        {
            "rfc2396" => "rfc2396",
            "rfc3986" => "rfc3986",
            "uuid" => "uuid",
            "strcmp0" => "strcmp0",
            "ldap" => "ldap",
            _ => null
        };
    }
}

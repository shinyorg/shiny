namespace Shiny.Net.Discovery.Managed.Ssdp;


/// <summary>
/// Decides whether an advertisement satisfies a search target. Used by the responder to pick what
/// to answer, and by the client to filter replies from devices that answer everything.
/// </summary>
static class SsdpMatcher
{
    /// <summary>
    /// True when a device advertising <paramref name="advertised"/> should answer a search for
    /// <paramref name="searchTarget"/>.
    /// </summary>
    public static bool Matches(string? searchTarget, string? advertised)
    {
        if (String.IsNullOrWhiteSpace(searchTarget) || String.IsNullOrWhiteSpace(advertised))
            return false;

        var target = searchTarget.Trim();
        var candidate = advertised.Trim();

        if (target.Equals(SsdpConstants.SearchAll, StringComparison.OrdinalIgnoreCase))
            return true;

        if (target.Equals(candidate, StringComparison.OrdinalIgnoreCase))
            return true;

        // a UDN search matches the device regardless of which of its advertisements we hold
        if (target.StartsWith("uuid:", StringComparison.OrdinalIgnoreCase) &&
            candidate.StartsWith("uuid:", StringComparison.OrdinalIgnoreCase))
        {
            return UsnParser.NormalizeUdn(target).Equals(UsnParser.NormalizeUdn(candidate), StringComparison.OrdinalIgnoreCase);
        }

        // UDA section 1.3.2: a device implementing version m answers a search for any version
        // n <= m. The reply still echoes the requested n.
        if (UsnParser.TryParseTypeUrn(target, out var wanted) &&
            UsnParser.TryParseTypeUrn(candidate, out var have))
        {
            return wanted.Version <= have.Version &&
                wanted.BaseUrn.Equals(have.BaseUrn, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }


    /// <summary>
    /// Builds the USN for an advertisement of <paramref name="notificationType"/> by
    /// <paramref name="udn"/>. A bare UDN advertisement carries no "::" suffix.
    /// </summary>
    public static string BuildUsn(string udn, string? notificationType)
        => String.IsNullOrWhiteSpace(notificationType) || notificationType.Equals(udn, StringComparison.OrdinalIgnoreCase)
            ? udn
            : $"{udn}::{notificationType}";
}

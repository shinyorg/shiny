namespace Shiny.Net.Discovery.Internals;


/// <summary>
/// A service type broken into its DNS-SD parts.
/// </summary>
/// <param name="Application">The application label including its underscore, eg "_http".</param>
/// <param name="Protocol">"_tcp" or "_udp".</param>
/// <param name="Domain">The domain without a trailing dot, eg "local".</param>
readonly record struct ServiceTypeInfo(string Application, string Protocol, string Domain)
{
    /// <summary>The type on its own, eg "_http._tcp".</summary>
    public string ServiceType => $"{this.Application}.{this.Protocol}";

    /// <summary>The type with its domain, eg "_http._tcp.local".</summary>
    public string QualifiedName => $"{this.ServiceType}.{this.Domain}";
}


/// <summary>
/// Parses and validates DNS-SD service types per RFC 6763 section 7.
/// </summary>
static class ServiceTypeParser
{
    public const int MaxApplicationLabelLength = 15;

    /// <summary>
    /// Parses a service type, throwing <see cref="MdnsException"/> when it is not valid.
    /// </summary>
    /// <param name="serviceType">
    /// Accepts "_http._tcp", "_http._tcp.", "_http._tcp.local" and "_http._tcp.local.".
    /// </param>
    /// <param name="domain">
    /// The domain to use when <paramref name="serviceType"/> does not carry one.
    /// </param>
    public static ServiceTypeInfo Parse(string serviceType, string domain = MdnsConstants.LocalDomain)
    {
        if (!TryParse(serviceType, domain, out var info, out var error))
            throw new MdnsException($"'{serviceType}' is not a valid DNS-SD service type. {error}");

        return info;
    }


    public static bool TryParse(string? serviceType, string domain, out ServiceTypeInfo info, out string? error)
    {
        info = default;
        error = null;

        if (String.IsNullOrWhiteSpace(serviceType))
        {
            error = "It is empty.";
            return false;
        }

        var labels = serviceType
            .Trim()
            .TrimEnd('.')
            .Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (labels.Length < 2)
        {
            error = "Expected an application label and a transport label, eg '_http._tcp'.";
            return false;
        }

        if (labels.Length > 2 && labels[1].Equals("_sub", StringComparison.OrdinalIgnoreCase))
        {
            error = "Service subtypes (_sub) are not supported.";
            return false;
        }

        var application = labels[0];
        var protocol = labels[1];

        if (!ValidateApplicationLabel(application, out error))
            return false;

        if (!protocol.Equals("_tcp", StringComparison.OrdinalIgnoreCase) &&
            !protocol.Equals("_udp", StringComparison.OrdinalIgnoreCase))
        {
            error = $"The transport label must be '_tcp' or '_udp' but was '{protocol}'.";
            return false;
        }

        // anything past the transport label is the domain, eg "_http._tcp.local"
        var parsedDomain = labels.Length > 2
            ? String.Join('.', labels[2..])
            : domain.Trim().TrimEnd('.');

        if (String.IsNullOrWhiteSpace(parsedDomain))
            parsedDomain = MdnsConstants.LocalDomain;

        info = new ServiceTypeInfo(
            application.ToLowerInvariant(),
            protocol.ToLowerInvariant(),
            parsedDomain
        );
        return true;
    }


    static bool ValidateApplicationLabel(string label, out string? error)
    {
        error = null;

        if (label.Length < 2 || label[0] != '_')
        {
            error = $"The application label '{label}' must start with an underscore, eg '_http'.";
            return false;
        }

        var name = label.AsSpan(1);
        if (name.Length > MaxApplicationLabelLength)
        {
            error = $"The application label '{label}' exceeds the {MaxApplicationLabelLength} character limit.";
            return false;
        }

        if (name[0] == '-' || name[^1] == '-')
        {
            error = $"The application label '{label}' cannot start or end with a hyphen.";
            return false;
        }

        var hasLetter = false;
        foreach (var c in name)
        {
            var isLetter = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
            hasLetter |= isLetter;

            if (!isLetter && !(c >= '0' && c <= '9') && c != '-')
            {
                error = $"The application label '{label}' may only contain letters, digits and hyphens.";
                return false;
            }
        }

        if (!hasLetter)
        {
            error = $"The application label '{label}' must contain at least one letter.";
            return false;
        }
        return true;
    }


    /// <summary>
    /// Validates an instance name. Instance names are a single DNS-SD label and may contain
    /// spaces and UTF8, but are capped at 63 bytes once encoded.
    /// </summary>
    public static void ValidateInstanceName(string? instanceName)
    {
        if (String.IsNullOrWhiteSpace(instanceName))
            throw new MdnsException("The instance name is required.");

        var byteCount = System.Text.Encoding.UTF8.GetByteCount(instanceName);
        if (byteCount > 63)
            throw new MdnsException($"The instance name '{instanceName}' encodes to {byteCount} bytes - a DNS label is limited to 63.");
    }


    /// <summary>
    /// Splits a fully qualified DNS-SD name ("Name._http._tcp.local") back into its instance
    /// name and type. Returns false when the name is not instance-qualified.
    /// </summary>
    public static bool TrySplitFullName(string fullName, out string instanceName, out ServiceTypeInfo info)
    {
        instanceName = String.Empty;
        info = default;

        var labels = fullName.TrimEnd('.').Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (labels.Length < 3)
            return false;

        // the transport label anchors the split - everything before it minus one label is the instance
        var protocolIndex = Array.FindIndex(
            labels,
            x => x.Equals("_tcp", StringComparison.OrdinalIgnoreCase) || x.Equals("_udp", StringComparison.OrdinalIgnoreCase)
        );
        if (protocolIndex < 2)
            return false;

        instanceName = String.Join('.', labels[..(protocolIndex - 1)]);
        var remainder = String.Join('.', labels[(protocolIndex - 1)..]);

        return TryParse(remainder, MdnsConstants.LocalDomain, out info, out _);
    }
}

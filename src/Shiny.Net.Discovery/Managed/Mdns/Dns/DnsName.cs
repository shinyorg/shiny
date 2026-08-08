using System.Text;

namespace Shiny.Net.Discovery.Managed.Dns;


/// <summary>
/// A DNS name held as its individual labels. DNS-SD instance names are a single label that may
/// legitimately contain dots ("Allan's 2.4Ghz Printer"), so names are never round-tripped
/// through a plain dotted string internally.
/// </summary>
sealed class DnsName : IEquatable<DnsName>
{
    public DnsName(params string[] labels)
        => this.Labels = labels;

    public string[] Labels { get; }

    public int Count => this.Labels.Length;

    public string this[int index] => this.Labels[index];


    /// <summary>
    /// Splits a dotted name into labels, honouring backslash escapes ("\." and "\\") the way
    /// DNS presentation format does.
    /// </summary>
    public static DnsName Parse(string name)
    {
        var labels = new List<string>();
        var current = new StringBuilder();
        var escaped = false;

        foreach (var c in name)
        {
            if (escaped)
            {
                current.Append(c);
                escaped = false;
            }
            else if (c == '\\')
            {
                escaped = true;
            }
            else if (c == '.')
            {
                if (current.Length > 0)
                    labels.Add(current.ToString());

                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        if (current.Length > 0)
            labels.Add(current.ToString());

        return new DnsName(labels.ToArray());
    }


    /// <summary>
    /// Builds the fully qualified name of a service instance. The instance name stays a single
    /// label no matter what characters it contains.
    /// </summary>
    public static DnsName ForInstance(string instanceName, string application, string protocol, string domain)
        => new([instanceName, application, protocol, .. domain.Split('.', StringSplitOptions.RemoveEmptyEntries)]);


    /// <summary>
    /// Builds the name of a service type, eg "_http._tcp.local".
    /// </summary>
    public static DnsName ForServiceType(string application, string protocol, string domain)
        => new([application, protocol, .. domain.Split('.', StringSplitOptions.RemoveEmptyEntries)]);


    // NUL cannot appear in a DNS label, unlike '.' or ' ', so it is the only unambiguous joiner
    const char SuffixSeparator = (char)0;

    /// <summary>
    /// The key used for name compression - identifies the suffix starting at <paramref name="startIndex"/>.
    /// </summary>
    public string SuffixKey(int startIndex)
        => String.Join(SuffixSeparator, this.Labels[startIndex..]).ToLowerInvariant();


    /// <summary>
    /// True when this name ends with <paramref name="suffix"/>, compared case-insensitively.
    /// </summary>
    public bool EndsWith(DnsName suffix)
    {
        if (suffix.Count > this.Count)
            return false;

        var offset = this.Count - suffix.Count;
        for (var i = 0; i < suffix.Count; i++)
        {
            if (!this.Labels[offset + i].Equals(suffix.Labels[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }


    public bool Equals(DnsName? other)
    {
        if (other == null || other.Count != this.Count)
            return false;

        for (var i = 0; i < this.Count; i++)
        {
            if (!this.Labels[i].Equals(other.Labels[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }


    public override bool Equals(object? obj) => this.Equals(obj as DnsName);


    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var label in this.Labels)
            hash.Add(label, StringComparer.OrdinalIgnoreCase);

        return hash.ToHashCode();
    }


    /// <summary>
    /// Presentation format with dots inside labels escaped, eg "My\.Printer._http._tcp.local".
    /// </summary>
    public override string ToString()
        => String.Join('.', this.Labels.Select(x => x.Replace("\\", "\\\\").Replace(".", "\\.")));


    /// <summary>
    /// The unescaped dotted form. Only safe for names whose labels contain no dots, ie host names.
    /// </summary>
    public string ToHostString() => String.Join('.', this.Labels);
}

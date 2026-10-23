using Tmds.DBus.Protocol;

namespace Shiny.Net.Vpn.NetworkManager;


/// <summary>
/// A NetworkManager connection profile - the <c>a{sa{sv}}</c> shape, kept as named setting groups
/// ("connection", "vpn", "ipv4", ...).
/// </summary>
/// <remarks>
/// Values are kept as plain .NET values rather than VariantValues so the profile a VpnProfile maps to
/// can be asserted on in tests. Only the types a VPN profile needs are supported: strings, booleans and
/// the <c>a{ss}</c> string maps the VPN plugin's data and secrets travel in.
/// </remarks>
internal sealed class NmConnectionSettings
{
    public Dictionary<string, Dictionary<string, object>> Groups { get; } = new();

    public Dictionary<string, object> Group(string name)
    {
        if (!this.Groups.TryGetValue(name, out var group))
        {
            group = new Dictionary<string, object>();
            this.Groups[name] = group;
        }
        return group;
    }
}


internal static class NmConnectionSettingsWriter
{
    /// <remarks>
    /// There is no VariantValue factory for a dictionary, so the nesting is written with the low-level
    /// dictionary primitives - and an <c>a{ss}</c> variant is its signature followed by the dictionary,
    /// which is all a variant is on the wire.
    /// </remarks>
    public static void WriteConnectionSettings(this ref MessageWriter writer, NmConnectionSettings settings)
    {
        var outer = writer.WriteDictionaryStart();
        foreach (var group in settings.Groups)
        {
            writer.WriteDictionaryEntryStart();
            writer.WriteString(group.Key);

            var inner = writer.WriteDictionaryStart();
            foreach (var entry in group.Value)
            {
                writer.WriteDictionaryEntryStart();
                writer.WriteString(entry.Key);
                writer.WriteValue(entry.Key, entry.Value);
            }
            writer.WriteDictionaryEnd(inner);
        }
        writer.WriteDictionaryEnd(outer);
    }


    static void WriteValue(this ref MessageWriter writer, string key, object value)
    {
        switch (value)
        {
            case string s:
                writer.WriteVariant(VariantValue.String(s));
                break;

            case bool b:
                writer.WriteVariant(VariantValue.Bool(b));
                break;

            case IReadOnlyDictionary<string, string> map:
                writer.WriteSignature("a{ss}");
                var start = writer.WriteDictionaryStart();
                foreach (var pair in map)
                {
                    writer.WriteDictionaryEntryStart();
                    writer.WriteString(pair.Key);
                    writer.WriteString(pair.Value);
                }
                writer.WriteDictionaryEnd(start);
                break;

            default:
                throw new InvalidOperationException($"Setting '{key}' has an unsupported type {value.GetType().Name}");
        }
    }
}

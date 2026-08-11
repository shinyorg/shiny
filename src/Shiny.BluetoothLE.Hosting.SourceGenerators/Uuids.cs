using System;
using System.Text;

namespace Shiny.BluetoothLE.Hosting.SourceGenerators;


/// <summary>
/// Bluetooth UUID validation and normalization.
/// </summary>
/// <remarks>
/// Everything is emitted in the full 128 bit form on purpose. Shiny's Apple backend goes through
/// <c>CBUUID.FromString</c>, which accepts short forms, but the Android backend goes through
/// <c>java.util.UUID.fromString</c>, which does not - a "180D" that works on iOS throws on Android.
/// Normalizing here means the same attribute value works on both, and it makes merge grouping
/// ("180D" and "0000180D-0000-1000-8000-00805F9B34FB" are the same service) fall out for free.
/// </remarks>
static class Uuids
{
    const string BluetoothBaseSuffix = "-0000-1000-8000-00805F9B34FB";


    /// <summary>
    /// Expands 16 and 32 bit forms to the full 128 bit UUID, uppercased.
    /// </summary>
    /// <param name="value">The value as written in the attribute.</param>
    /// <param name="normalized">The 128 bit form, when the input was valid.</param>
    /// <returns>True when <paramref name="value"/> is a usable Bluetooth UUID.</returns>
    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = String.Empty;
        if (String.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value!.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed.Substring(2);

        switch (trimmed.Length)
        {
            case 4 when IsHex(trimmed):
                normalized = "0000" + trimmed.ToUpperInvariant() + BluetoothBaseSuffix;
                return true;

            case 8 when IsHex(trimmed):
                normalized = trimmed.ToUpperInvariant() + BluetoothBaseSuffix;
                return true;

            case 36 when Guid.TryParseExact(trimmed, "D", out var parsed):
                normalized = parsed.ToString("D").ToUpperInvariant();
                return true;

            case 38 when trimmed[0] == '{' && Guid.TryParseExact(trimmed, "B", out var braced):
                normalized = braced.ToString("D").ToUpperInvariant();
                return true;

            default:
                return false;
        }
    }


    /// <summary>
    /// Builds an identifier-safe suffix for generated fields and methods from a normalized UUID.
    /// Uses the distinguishing 32 bit prefix when the UUID sits on the Bluetooth base, otherwise
    /// the whole thing with the dashes stripped.
    /// </summary>
    /// <param name="normalizedUuid">A UUID that already went through <see cref="TryNormalize"/>.</param>
    /// <returns>An identifier fragment.</returns>
    public static string ToIdentifier(string normalizedUuid)
    {
        if (normalizedUuid.EndsWith(BluetoothBaseSuffix, StringComparison.OrdinalIgnoreCase))
        {
            var prefix = normalizedUuid.Substring(0, 8);
            // trim the leading zeros of a 16 bit UUID so 0000180D reads as 180D
            return prefix.StartsWith("0000", StringComparison.Ordinal) ? prefix.Substring(4) : prefix;
        }

        var builder = new StringBuilder(32);
        foreach (var c in normalizedUuid)
        {
            if (c != '-')
                builder.Append(c);
        }
        return builder.ToString();
    }


    static bool IsHex(string value)
    {
        foreach (var c in value)
        {
            var isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
            if (!isHex)
                return false;
        }
        return true;
    }
}

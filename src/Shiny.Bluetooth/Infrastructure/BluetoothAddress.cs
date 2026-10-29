using System.Globalization;

namespace Shiny.Bluetooth;


/// <summary>
/// Converts between the address forms the platforms use - <c>AA:BB:CC:DD:EE:FF</c> (Android, BlueZ),
/// <c>aa-bb-cc-dd-ee-ff</c> (IOBluetooth), <c>(aa:bb:cc:dd:ee:ff)</c> (a WinRT host name) and a 48-bit
/// integer (WinRT <c>BluetoothAddress</c>).
/// </summary>
internal static class BluetoothAddress
{
    public static string Normalize(string address)
    {
        ArgumentNullException.ThrowIfNull(address);

        Span<char> hex = stackalloc char[12];
        var count = 0;
        foreach (var c in address)
        {
            if (Uri.IsHexDigit(c))
            {
                if (count == 12)
                    throw Invalid(address);

                hex[count++] = char.ToUpperInvariant(c);
            }
            else if (c is not (':' or '-' or '(' or ')' or ' '))
            {
                throw Invalid(address);
            }
        }
        if (count != 12)
            throw Invalid(address);

        return string.Create(17, hex.ToString(), static (span, digits) =>
        {
            for (var i = 0; i < 6; i++)
            {
                span[i * 3] = digits[i * 2];
                span[i * 3 + 1] = digits[i * 2 + 1];
                if (i < 5)
                    span[i * 3 + 2] = ':';
            }
        });
    }


    public static ulong ToUInt64(string address)
        => ulong.Parse(Normalize(address).Replace(":", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture);


    public static string FromUInt64(ulong address)
        => Normalize((address & 0xFFFF_FFFF_FFFF).ToString("X12", CultureInfo.InvariantCulture));


    /// <summary>The IOBluetooth form, <c>aa-bb-cc-dd-ee-ff</c>.</summary>
    public static string ToDashed(string address)
        => Normalize(address).Replace(':', '-').ToLowerInvariant();


    static ArgumentException Invalid(string address)
        => new($"'{address}' is not a Bluetooth device address", nameof(address));
}

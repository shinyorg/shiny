using System.Text;

namespace Shiny.Bluetooth;


/// <summary>
/// The few SDP data elements a published record needs written by hand (Windows takes raw attribute bytes).
/// </summary>
internal static class SdpEncoding
{
    /// <summary>The ServiceName attribute id, at the default language base (0x0100).</summary>
    public const ushort ServiceNameAttributeId = 0x0100;


    /// <summary>
    /// Encodes a text string data element - type 4 with an 8-bit length (0x25), the length, then UTF-8.
    /// Names longer than 255 bytes are truncated on a character boundary.
    /// </summary>
    public static byte[] Text(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var length = bytes.Length;
        if (length > byte.MaxValue)
        {
            length = byte.MaxValue;
            // back off any UTF-8 continuation bytes so a multi-byte character is not split
            while (length > 0 && (bytes[length] & 0xC0) == 0x80)
                length--;
        }

        var result = new byte[length + 2];
        result[0] = 0x25;
        result[1] = (byte)length;
        Array.Copy(bytes, 0, result, 2, length);
        return result;
    }
}

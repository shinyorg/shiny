namespace Shiny.Tests;


public static class Utils
{
#if ANDROID
    public static AndroidPlatform GetPlatform() => new AndroidPlatform();
#elif IOS || MACCATALYST
    public static IosPlatform GetPlatform() => new IosPlatform();
#endif


    /// <summary>
    /// Converts a HEX string to a byte array
    /// </summary>
    /// <param name="hex"></param>
    /// <returns></returns>
    public static byte[] FromHex(this string hex)
    {
        hex = hex
            .Replace("-", String.Empty)
            .Replace(" ", String.Empty);

        return Enumerable
            .Range(0, hex.Length)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
            .ToArray();
    }


    /// <summary>
    /// Convert a byte array to HEX string
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static string ToHex(this byte[] bytes)
        => String.Concat(bytes.Select(b => b.ToString("X2")));
}

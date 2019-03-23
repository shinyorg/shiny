using System;
using System.Linq;


namespace Shiny
{
    public static class Extensions_String
    {
        public static bool IsEmpty(this string s) => String.IsNullOrWhiteSpace(s);


        public static bool HasMinLength(this string @string, int length)
        {
            if (@string.IsEmpty())
                return false;

            return @string.Length >= length;
        }


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


        public static string ToHex(this byte[] bytes)
            => String.Concat(bytes.Select(b => b.ToString("X2")));
    }
}

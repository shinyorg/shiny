using System;
using System.Linq;


namespace Shiny
{
    public static class Extensions_String
    {
        /// <summary>
        /// Extension method to String.IsNullOrWhiteSpace
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsEmpty(this string s) => String.IsNullOrWhiteSpace(s);


        /// <summary>
        /// Safetied string length check
        /// </summary>
        /// <param name="string"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static bool HasMinLength(this string @string, int length)
        {
            if (@string.IsEmpty())
                return false;

            return @string.Length >= length;
        }


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
}

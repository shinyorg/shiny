using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;


namespace Shiny
{
    public static partial class Extensions
    {
        public static string ResourceToFilePath(this IPlatform platform, Assembly assembly, string resourceName)
        {
            var path = Path.Combine(platform.AppData.FullName, resourceName);
            if (!File.Exists(path))
            {
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    using (var fs = File.Create(path))
                    {
                        stream.CopyTo(fs);
                    }
                }
            }
            return path;
        }


        /// <summary>
        /// Extension method to String.IsNullOrWhiteSpace
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsEmpty(this string? s) => String.IsNullOrWhiteSpace(s);


        /// <summary>
        /// Safetied string length check
        /// </summary>
        /// <param name="string"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static bool HasMinLength(this string? @string, int length)
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


        public static void Assert(this AccessState state, string? message = null, bool allowRestricted = false)
        {
            if (state == AccessState.Available)
                return;

            if (allowRestricted && state == AccessState.Restricted)
                return;

            throw new ArgumentException(message ?? $"Invalid State " + state);
        }


        public static IEnumerable<IEnumerable<T>> Page<T>(this IEnumerable<T> source, int pageSize)
        {
            Contract.Requires(source != null);
            Contract.Requires(pageSize > 0);
            Contract.Ensures(Contract.Result<IEnumerable<IEnumerable<T>>>() != null);

            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var currentPage = new List<T>(pageSize)
                    {
                        enumerator.Current
                    };

                    while (currentPage.Count < pageSize && enumerator.MoveNext())
                    {
                        currentPage.Add(enumerator.Current);
                    }
                    yield return new ReadOnlyCollection<T>(currentPage);
                }
            }
        }


        /// <summary>
        /// Turns a paired key tuple into a dictionary
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="tuples"></param>
        /// <returns></returns>
        public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<(TKey, TValue)> tuples)
        {
            var dict = new Dictionary<TKey, TValue>();
            if (tuples != null)
                foreach (var tuple in tuples)
                    dict.Add(tuple.Item1, tuple.Item2);

            return dict;
        }


        /// <summary>
        /// A safe dictionary Get, will return a default value if the dictionary does not contain the key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T Get<T>(this IDictionary<string, object> dict, string key, T defaultValue = default)
        {
            if (!dict.ContainsKey(key))
                return defaultValue;

            var obj = dict[key];
            if (obj is T)
                return (T)obj;

            return defaultValue;
        }


        /// <summary>
        /// Safely checks an enumerable if it is null or has no elements
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="en"></param>
        /// <returns></returns>
        public static bool IsEmpty<T>(this IEnumerable<T> en)
            => en == null || !en.Any();



        /// <summary>
        /// Creates a new array and copies the elements from both arrays together
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public static T[] Expand<T>(this T[] array, params T[] items)
        {
            array = array ?? new T[0];
            var newArray = new T[array.Length + items.Length];
            Array.Copy(array, newArray, array.Length);
            Array.Copy(items, 0, newArray, array.Length, items.Length);
            return newArray;
        }
    }
}

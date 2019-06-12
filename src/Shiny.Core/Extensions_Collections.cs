using System;
using System.Collections.Generic;
using System.Linq;


namespace Shiny
{
    public static partial class Extensions
    {
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

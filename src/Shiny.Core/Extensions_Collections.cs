using System;
using System.Collections.Generic;
using System.Linq;


namespace Shiny
{
    public static class Extensions_Collections
    {
        public static T Get<T>(this IDictionary<string, object> dict, string key, T defaultValue = default)
        {
            if (!dict.ContainsKey(key))
                return defaultValue;

            var obj = dict[key];
            if (obj is T)
                return (T)obj;

            return defaultValue;
        }


        public static bool IsEmpty<T>(this IEnumerable<T> en)
            => en == null || !en.Any();


        public static void Each<T>(this IEnumerable<T> en, Action<T> action)
        {
            if (en == null)
                return;

            foreach (var obj in en)
                action(obj);
        }


        public static void Each<T>(this IEnumerable<T> en, Action<int, T> action)
        {
            if (en == null)
                return;

            var i = 0;
            foreach (var obj in en)
            {
                action(i, obj);
                i++;
            }
        }


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

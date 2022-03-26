using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;


namespace Shiny
{
    public static class GeneralExtensions
    {
        /// <summary>
        /// Filters the list if the expression is not null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="en"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> en, Expression<Func<T, bool>>? expression)
            => expression == null ? en : en.Where(expression.Compile());


        /// <summary>
        /// Uses reflection to get a property value from an object by name
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static object GetValue(this object obj, string propertyName)
        {
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop == null)
                throw new InvalidOperationException($"Property '{propertyName}' does not exist at '{obj.GetType().FullName}");

            var result = prop.GetValue(obj, null);
            return result;
        }


        /// <summary>
        /// Gets the property info for an expression
        /// </summary>
        /// <typeparam name="TSender"></typeparam>
        /// <typeparam name="TRet"></typeparam>
        /// <param name="sender"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static PropertyInfo GetPropertyInfo<TSender, TRet>(this TSender sender, Expression<Func<TSender, TRet>> expression)
        {
            if (sender == null)
                throw new ArgumentException("Sender is null");

            var member = (expression as LambdaExpression)?.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException("Invalid lamba expression - body is not a member expression");

            var property = sender.GetType().GetRuntimeProperty(member.Member.Name);
            return property;
        }


        /// <summary>
        /// Gets the default value for a type
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static object? GetDefaultValue(this Type t)
        {
            if (t.IsValueType && Nullable.GetUnderlyingType(t) == null)
                return Activator.CreateInstance(t);

            return null;
        }

        /// <summary>
        /// Extension method to String.IsNullOrWhiteSpace
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsEmpty(this string? s) => String.IsNullOrWhiteSpace(s);


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


        /// <summary>
        /// Asserts that AccessState is available (or allows restricted)
        /// </summary>
        /// <param name="state"></param>
        /// <param name="message"></param>
        /// <param name="allowRestricted"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void Assert(this AccessState state, string? message = null, bool allowRestricted = false)
        {
            if (state == AccessState.Available)
                return;

            if (allowRestricted && state == AccessState.Restricted)
                return;

            throw new InvalidOperationException(message ?? $"Invalid State " + state);
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
            { 
                foreach (var tuple in tuples)
                    dict.Add(tuple.Item1, tuple.Item2);
            }
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
            if (dict == null)
                return defaultValue;

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

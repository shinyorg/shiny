using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Shiny;


public static class GeneralExtensions
{
    /// <summary>
    /// Extension method to String.IsNullOrWhiteSpace
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static bool IsEmpty(this string? s) => String.IsNullOrWhiteSpace(s);


    static readonly Dictionary<Type, object> KnownDefaults = new()
    {
        [typeof(bool)]           = false,
        [typeof(byte)]           = (byte)0,
        [typeof(char)]           = default(char),
        [typeof(DateTime)]       = default(DateTime),
        [typeof(DateTimeOffset)] = default(DateTimeOffset),
        [typeof(decimal)]        = default(decimal),
        [typeof(double)]         = default(double),
        [typeof(float)]          = default(float),
        [typeof(Guid)]           = default(Guid),
        [typeof(int)]            = 0,
        [typeof(long)]           = 0L,
        [typeof(sbyte)]          = (sbyte)0,
        [typeof(short)]          = (short)0,
        [typeof(TimeSpan)]       = default(TimeSpan),
        [typeof(uint)]           = 0u,
        [typeof(ulong)]          = 0ul,
        [typeof(ushort)]         = (ushort)0,
    };


    /// <summary>
    /// Gets the default value for a type without using reflection.
    /// Supports all primitive value types and common BCL structs.
    /// </summary>
    public static object? GetDefaultValue(this Type t)
    {
        if (!t.IsValueType || Nullable.GetUnderlyingType(t) != null)
            return null;

        if (KnownDefaults.TryGetValue(t, out var val))
            return val;

        throw new NotSupportedException($"Cannot determine default value for struct type '{t.FullName}'. Add it to KnownDefaults.");
    }
    
    
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
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Shiny;


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
    /// Extension method to String.IsNullOrWhiteSpace
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static bool IsEmpty(this string? s) => String.IsNullOrWhiteSpace(s);


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
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Shiny.Reflection;


public static class Extensions
{

    /// <summary>
    /// Uses reflection to get a property value from an object by name
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static object? GetValue(this object obj, string propertyName)
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
    public static PropertyInfo? GetPropertyInfo<TSender, TRet>(this TSender sender, Expression<Func<TSender, TRet>> expression)
    {
        if (sender == null)
            throw new ArgumentException("Sender is null");

        var member = expression.Body as MemberExpression;
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
}
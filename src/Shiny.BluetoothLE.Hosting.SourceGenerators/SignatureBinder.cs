using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Shiny.BluetoothLE.Hosting.SourceGenerators;


/// <summary>
/// What a handler is allowed to hand back.
/// </summary>
[Flags]
enum AllowedResults
{
    None = 1,
    Bytes = 2,
    GattResult = 4,
    GattState = 8
}


/// <summary>
/// Maps a user's handler method onto the fixed shape the hosting builders expect: which argument
/// expression to pass for each declared parameter, and how to consume the return value.
/// </summary>
static class SignatureBinder
{
    /// <summary>
    /// Binds <paramref name="method"/> against <paramref name="bindings"/>.
    /// </summary>
    /// <param name="method">The user's handler.</param>
    /// <param name="kind">Handler kind, used in diagnostic messages.</param>
    /// <param name="bindings">Parameter type (fully qualified, or the sentinel keys below) to argument expression.</param>
    /// <param name="allowed">Return shapes this handler kind accepts.</param>
    /// <param name="diagnostics">Collects anything that did not line up.</param>
    /// <param name="contextTypeFullName">
    /// The service's generated context type, when there is one. It does not exist as a symbol yet
    /// unless the user declared their own half of the partial, so it is matched by name as well.
    /// </param>
    /// <returns>The bound handler, or null when it could not be bound.</returns>
    public static HandlerModel? Bind(
        IMethodSymbol method,
        string kind,
        IReadOnlyDictionary<string, string> bindings,
        AllowedResults allowed,
        List<DiagnosticInfo> diagnostics,
        string? contextTypeFullName = null
    )
    {
        var location = LocationInfo.From(method);

        if (method.IsStatic || method.IsAbstract || method.IsGenericMethod ||
            method.Parameters.Any(p => p.RefKind != RefKind.None))
        {
            diagnostics.Add(DiagnosticInfo.Create(Diagnostics.InvalidHandlerModifier, location, method.Name));
            return null;
        }

        var contextSimpleName = contextTypeFullName?.Substring(contextTypeFullName.LastIndexOf('.') + 1);

        var arguments = new List<string>(method.Parameters.Length);
        foreach (var parameter in method.Parameters)
        {
            var key = BindingKey(parameter.Type);

            // the context type only becomes a real symbol once this generator emits it, so a handler
            // that names it while the user has not written their own half arrives as an error type
            if (key != null && !bindings.ContainsKey(key) &&
                parameter.Type is IErrorTypeSymbol && parameter.Type.Name == contextSimpleName)
                key = contextTypeFullName;

            if (key == null || !bindings.TryGetValue(key, out var expression))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    Diagnostics.UnsupportedSignature,
                    location,
                    method.Name,
                    kind,
                    $"parameter '{parameter.Name}' of type '{parameter.Type.ToDisplayString()}' has nothing to bind to (available: {String.Join(", ", bindings.Keys.Select(Friendly))})"
                ));
                return null;
            }
            arguments.Add(expression);
        }

        if (!TryBindResult(method.ReturnType, out var result, out var isAwaitable) || (allowed & ToFlag(result)) == 0)
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Diagnostics.UnsupportedSignature,
                location,
                method.Name,
                kind,
                $"return type '{method.ReturnType.ToDisplayString()}' is not supported here (allowed: {Describe(allowed)})"
            ));
            return null;
        }

        return new HandlerModel(method.Name, arguments.ToEquatableArray(), result, isAwaitable);
    }


    /// <summary>
    /// Reduces a parameter type to the key used in the binding tables. Returns null for types that
    /// are never bindable.
    /// </summary>
    public static string? BindingKey(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
            return "byte[]";

        return type.SpecialType switch
        {
            SpecialType.System_Int32 => "int",
            SpecialType.System_Boolean => "bool",
            _ => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        };
    }


    static bool TryBindResult(ITypeSymbol returnType, out HandlerResult result, out bool isAwaitable)
    {
        isAwaitable = false;
        result = HandlerResult.None;

        var type = returnType;
        if (type.SpecialType == SpecialType.System_Void)
            return true;

        // compare on name + namespace rather than the display string, which carries the arity for Task<T>
        if (type is INamedTypeSymbol named &&
            named.Name is "Task" or "ValueTask" &&
            named.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks")
        {
            isAwaitable = true;
            if (named.TypeArguments.Length != 1)
                return true; // bare Task / ValueTask

            type = named.TypeArguments[0];
        }

        if (type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
        {
            result = HandlerResult.Bytes;
            return true;
        }

        result = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) switch
        {
            Names.GattResult => HandlerResult.GattResult,
            Names.GattState => HandlerResult.GattState,
            _ => HandlerResult.None
        };

        // a non-void, non-awaitable type we do not recognise is a hard no
        return result != HandlerResult.None;
    }


    static AllowedResults ToFlag(HandlerResult result) => result switch
    {
        HandlerResult.Bytes => AllowedResults.Bytes,
        HandlerResult.GattResult => AllowedResults.GattResult,
        HandlerResult.GattState => AllowedResults.GattState,
        _ => AllowedResults.None
    };


    static string Describe(AllowedResults allowed)
    {
        var parts = new List<string>(4);
        if ((allowed & AllowedResults.None) != 0) parts.Add("void, Task, ValueTask");
        if ((allowed & AllowedResults.Bytes) != 0) parts.Add("byte[]");
        if ((allowed & AllowedResults.GattResult) != 0) parts.Add("GattResult");
        if ((allowed & AllowedResults.GattState) != 0) parts.Add("GattState");

        var listed = String.Join(", ", parts);
        return (allowed & ~AllowedResults.None) == 0 ? listed : listed + " (each optionally wrapped in Task/ValueTask)";
    }


    static string Friendly(string key)
    {
        var index = key.LastIndexOf('.');
        return index < 0 ? key : key.Substring(index + 1);
    }
}

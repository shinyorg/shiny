using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Shiny.BluetoothLE.Hosting.SourceGenerators;


/// <summary>
/// A <see cref="Location"/> flattened to value-equatable parts so it can ride the incremental pipeline.
/// </summary>
sealed record LocationInfo(string FilePath, TextSpan TextSpan, Microsoft.CodeAnalysis.Text.LinePositionSpan LineSpan)
{
    public Location ToLocation() => Location.Create(this.FilePath, this.TextSpan, this.LineSpan);


    public static LocationInfo? From(SyntaxNode? node)
        => node == null ? null : From(node.GetLocation());


    public static LocationInfo? From(Location? location)
        => location?.SourceTree == null
            ? null
            : new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);


    public static LocationInfo? From(ISymbol symbol)
        => From(symbol.Locations.FirstOrDefault());
}


/// <summary>
/// A deferred diagnostic. Held as data so cross-class checks can run in the output stage.
/// </summary>
sealed record DiagnosticInfo(DiagnosticDescriptor Descriptor, LocationInfo? Location, EquatableArray<string> MessageArgs)
{
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, LocationInfo? location, params string[] args)
        => new(descriptor, location, new EquatableArray<string>(args));


    public Diagnostic ToDiagnostic()
        => Diagnostic.Create(this.Descriptor, this.Location?.ToLocation(), this.MessageArgs.Cast<object?>().ToArray());
}


/// <summary>
/// What a handler hands back, once <c>Task</c>/<c>ValueTask</c> has been peeled off.
/// </summary>
enum HandlerResult
{
    /// <summary>void / Task / ValueTask</summary>
    None,

    /// <summary>byte[]</summary>
    Bytes,

    /// <summary>GattResult</summary>
    GattResult,

    /// <summary>GattState</summary>
    GattState
}


/// <summary>
/// A bound handler method: the name to call, the argument expressions to call it with, and how to
/// consume what it returns.
/// </summary>
sealed record HandlerModel(
    string MethodName,
    EquatableArray<string> Arguments,
    HandlerResult Result,
    bool IsAwaitable
)
{
    public string Invocation => $"this.{this.MethodName}({String.Join(", ", this.Arguments)})";
}


/// <summary>
/// One characteristic inside a service class, with whichever handlers were declared for it.
/// </summary>
sealed record CharacteristicModel(
    string Uuid,
    string FieldName,
    HandlerModel? Read,
    bool ReadEncrypted,
    HandlerModel? Write,
    string WriteOptionsExpression,
    bool ManualRespond,
    HandlerModel? RequestResponse,
    bool HasNotify,
    HandlerModel? NotifyHook,
    string? NotifyName,
    string NotificationOptionsExpression,
    LocationInfo? Location
);


/// <summary>
/// A class carrying <c>[BleService]</c>.
/// </summary>
sealed record ServiceModel(
    string ClassName,
    string Namespace,
    string FullyQualifiedName,
    string ContextTypeName,
    string ContextFullyQualifiedName,
    string Accessibility,
    string ServiceUuid,
    bool Primary,
    bool Advertise,
    string RegistrationName,
    bool NameIsExplicit,
    EquatableArray<CharacteristicModel> Characteristics,
    LocationInfo? Location
);


/// <summary>
/// A class carrying <c>[L2CapService]</c>.
/// </summary>
sealed record L2CapModel(
    string ClassName,
    string Namespace,
    string FullyQualifiedName,
    string Accessibility,
    bool Secure,
    string? PsmServiceUuid,
    string? PsmCharacteristicUuid,
    string RegistrationName,
    HandlerModel? Handler,
    LocationInfo? Location
);


/// <summary>
/// A parsed declaration plus anything that went wrong while parsing it. The model is null when the
/// declaration could not be salvaged.
/// </summary>
sealed record ParseResult<T>(T? Model, EquatableArray<DiagnosticInfo> Diagnostics)
    where T : class;

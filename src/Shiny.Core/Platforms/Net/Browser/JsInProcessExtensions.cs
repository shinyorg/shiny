using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Shiny.Infrastructure;


static class JsInProcessExtensions
{
    // matches JSInterop's own JsonSerialized annotation on InvokeAsync<TValue>
    const DynamicallyAccessedMemberTypes JsonSerialized =
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.PublicFields |
        DynamicallyAccessedMemberTypes.PublicProperties;


    /// <summary>
    /// A synchronous call with no arguments. <c>Invoke&lt;T&gt;</c> requires unreferenced code because its
    /// <c>params object[]</c> arguments are serialized by their runtime type; with none, only <typeparamref name="T"/>
    /// is serialized, and the annotation keeps what it needs.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "No arguments are passed; the result type is annotated")]
    public static T InvokeNoArgs<[DynamicallyAccessedMembers(JsonSerialized)] T>(this IJSInProcessObjectReference js, string identifier)
        => js.Invoke<T>(identifier, []);
}

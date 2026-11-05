using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Implementation;

namespace Shiny.Infrastructure;


/// <summary>
/// The JS object reference <see cref="UnoJSRuntime"/> hands out. In-process, like Blazor WebAssembly's: Shiny's browser
/// modules import their script as an <see cref="IJSObjectReference"/> and read it synchronously through
/// <c>mod as IJSInProcessObjectReference</c>, which only works when the runtime returns an in-process reference.
/// </summary>
sealed class UnoJSObjectReference(UnoJSRuntime runtime, long id) : JSInProcessObjectReference(runtime, id);


/// <summary>
/// Makes every <see cref="IJSObjectReference"/> the runtime deserializes an <see cref="UnoJSObjectReference"/> - what
/// Blazor's <c>WebAssemblyJSObjectReferenceJsonConverter</c> does. Without it Microsoft.JSInterop returns its plain,
/// asynchronous-only reference and the synchronous reads above see null.
/// </summary>
sealed class UnoJSObjectReferenceJsonConverter(UnoJSRuntime runtime) : JsonConverter<IJSObjectReference>
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert == typeof(IJSObjectReference) || typeToConvert == typeof(UnoJSObjectReference);

    public override IJSObjectReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new UnoJSObjectReference(runtime, JSObjectReferenceJsonWorker.ReadJSObjectReferenceIdentifier(ref reader));

    public override void Write(Utf8JsonWriter writer, IJSObjectReference value, JsonSerializerOptions options)
        => JSObjectReferenceJsonWorker.WriteJSObjectReference(writer, (JSObjectReference)value);
}

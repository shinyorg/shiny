using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.JSInterop.Infrastructure;

namespace Shiny.Infrastructure;


/// <summary>
/// The two directions of <see cref="UnoJSRuntime"/>: the imports call wwwroot/shiny-uno-interop.js, which hands them
/// to Microsoft.JSInterop's dispatcher, and the dispatcher calls the exports back when JS invokes .NET.
/// </summary>
[SupportedOSPlatform("browser")]
static partial class UnoJSInterop
{
    internal const string ModuleName = "Shiny.Hosting.Uno.WebAssembly";


    [JSImport("init", ModuleName)]
    internal static partial Task Init(string assemblyName);

    [JSImport("invokeJS", ModuleName)]
    internal static partial string? InvokeJS(
        string identifier,
        string? argsJson,
        int resultType,
        [JSMarshalAs<JSType.Number>] long targetInstanceId,
        int callType
    );

    [JSImport("beginInvokeJS", ModuleName)]
    internal static partial void BeginInvokeJS(
        [JSMarshalAs<JSType.Number>] long asyncHandle,
        string identifier,
        string? argsJson,
        int resultType,
        [JSMarshalAs<JSType.Number>] long targetInstanceId,
        int callType
    );

    [JSImport("endInvokeDotNet", ModuleName)]
    internal static partial void EndInvokeDotNet(string? callId, bool succeeded, string? resultJsonOrError);

    [JSImport("receiveByteArray", ModuleName)]
    internal static partial void ReceiveByteArrayInJS(int id, byte[] data);


    [JSExport]
    internal static void BeginInvokeDotNet(
        string? callId,
        string? assemblyName,
        string methodIdentifier,
        [JSMarshalAs<JSType.Number>] long dotNetObjectId,
        string argsJson
    ) => DotNetDispatcher.BeginInvokeDotNet(
        UnoJSRuntime.Instance,
        new DotNetInvocationInfo(assemblyName, methodIdentifier, dotNetObjectId, callId),
        argsJson
    );


    [JSExport]
    internal static string? InvokeDotNet(
        string? assemblyName,
        string methodIdentifier,
        [JSMarshalAs<JSType.Number>] long dotNetObjectId,
        string argsJson
    ) => DotNetDispatcher.Invoke(
        UnoJSRuntime.Instance,
        new DotNetInvocationInfo(assemblyName, methodIdentifier, dotNetObjectId, null),
        argsJson
    );


    [JSExport]
    internal static void EndInvokeJS(string serialized)
        => DotNetDispatcher.EndInvokeJS(UnoJSRuntime.Instance, serialized);


    [JSExport]
    internal static void ReceiveByteArray(int id, byte[] data)
        => DotNetDispatcher.ReceiveByteArray(UnoJSRuntime.Instance, id, data);
}

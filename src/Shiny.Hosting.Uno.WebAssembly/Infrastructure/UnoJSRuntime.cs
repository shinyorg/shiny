using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Shiny.Infrastructure;


/// <summary>
/// An <see cref="IJSRuntime"/> for Uno's WebAssembly head - the role Blazor's <c>WebAssemblyJSRuntime</c> plays in a
/// Blazor app. Shiny's browser modules are written against <see cref="IJSRuntime"/>: they import their scripts from
/// <c>_content/Shiny.*</c>, call them in and out of process, and receive events through
/// <see cref="DotNetObjectReference"/>. This runs all of it through Microsoft.JSInterop's own JS dispatcher, attached by
/// wwwroot/shiny-uno-interop.js over the runtime's <c>[JSImport]</c> / <c>[JSExport]</c> interop.
/// </summary>
/// <remarks>
/// The interop script loads asynchronously. Asynchronous calls made before it has loaded wait for it; a synchronous
/// (in-process) call that early throws. Shiny's modules always start with an asynchronous <c>import</c>, so they never
/// make one.
/// </remarks>
[SupportedOSPlatform("browser")]
sealed class UnoJSRuntime : JSInProcessRuntime
{
    static readonly object sync = new();
    static UnoJSRuntime? instance;

    readonly Task ready;


    UnoJSRuntime()
    {
        // ahead of Microsoft.JSInterop's own converter, as Blazor's runtime does: module imports come back in-process
        this.JsonSerializerOptions.Converters.Insert(0, new UnoJSObjectReferenceJsonConverter(this));
        this.ready = LoadAsync();
    }


    internal static UnoJSRuntime Instance
        => instance ?? throw new InvalidOperationException("Shiny's Uno WebAssembly JS runtime has not been created - call UseShinyWebAssembly() on the host builder");


    internal static UnoJSRuntime GetOrCreate()
    {
        lock (sync)
            return instance ??= new UnoJSRuntime();
    }


    static async Task LoadAsync()
    {
        // resolved against the document, like Blazor's "./_content/..." imports, so an app under a sub-path works
        var baseUri = JSHost.GlobalThis.GetPropertyAsJSObject("document")?.GetPropertyAsString("baseURI")
            ?? throw new InvalidOperationException("document.baseURI is not available");

        var url = new Uri(new Uri(baseUri), "_content/Shiny.Hosting.Uno.WebAssembly/shiny-uno-interop.js");
        await JSHost.ImportAsync(UnoJSInterop.ModuleName, url.ToString()).ConfigureAwait(false);
        await UnoJSInterop.Init(typeof(UnoJSRuntime).Assembly.GetName().Name!).ConfigureAwait(false);
    }


    protected override string? InvokeJS(string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
        => this.Invoke(identifier, argsJson, resultType, targetInstanceId, JSCallType.FunctionCall);


    protected override string? InvokeJS(in JSInvocationInfo invocationInfo)
        => this.Invoke(
            invocationInfo.Identifier,
            invocationInfo.ArgsJson,
            invocationInfo.ResultType,
            invocationInfo.TargetInstanceId,
            invocationInfo.CallType
        );


    protected override void BeginInvokeJS(long taskId, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
        => this.BeginInvoke(taskId, identifier, argsJson, resultType, targetInstanceId, JSCallType.FunctionCall);


    protected override void BeginInvokeJS(in JSInvocationInfo invocationInfo)
        => this.BeginInvoke(
            invocationInfo.AsyncHandle,
            invocationInfo.Identifier,
            invocationInfo.ArgsJson,
            invocationInfo.ResultType,
            invocationInfo.TargetInstanceId,
            invocationInfo.CallType
        );


    protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
        => UnoJSInterop.EndInvokeDotNet(
            invocationInfo.CallId,
            invocationResult.Success,
            invocationResult.Success ? invocationResult.ResultJson : invocationResult.Exception?.ToString()
        );


    protected override void SendByteArray(int id, byte[] data)
        => UnoJSInterop.ReceiveByteArrayInJS(id, data);


    string? Invoke(string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId, JSCallType callType)
    {
        if (!this.ready.IsCompletedSuccessfully)
            throw new InvalidOperationException($"Shiny's Uno WebAssembly JS interop has not loaded yet - '{identifier}' was called synchronously before any asynchronous call completed");

        return UnoJSInterop.InvokeJS(identifier, argsJson, (int)resultType, targetInstanceId, (int)callType);
    }


    void BeginInvoke(long asyncHandle, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId, JSCallType callType)
    {
        if (this.ready.IsCompletedSuccessfully)
            UnoJSInterop.BeginInvokeJS(asyncHandle, identifier, argsJson, (int)resultType, targetInstanceId, (int)callType);
        else
            _ = this.BeginInvokeWhenReady(asyncHandle, identifier, argsJson, resultType, targetInstanceId, callType);
    }


    async Task BeginInvokeWhenReady(long asyncHandle, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId, JSCallType callType)
    {
        try
        {
            await this.ready.ConfigureAwait(false);
            UnoJSInterop.BeginInvokeJS(asyncHandle, identifier, argsJson, (int)resultType, targetInstanceId, (int)callType);
        }
        catch (Exception ex)
        {
            // fail the pending call the way the JS dispatcher reports an error: [asyncHandle, false, "message"]
            if (asyncHandle != 0)
                DotNetDispatcher.EndInvokeJS(this, $"[{asyncHandle},false,\"{JsonEncodedText.Encode(ex.Message)}\"]");
        }
    }
}

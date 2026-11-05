// Connects Microsoft.JSInterop's JS dispatcher to UnoJSRuntime - the job blazor.webassembly.js does in a Blazor app.
// .NET -> JS: UnoJSInterop's [JSImport]s below. JS -> .NET (DotNetObjectReference.invokeMethodAsync and friends):
// the dispatcher calls back into UnoJSInterop's [JSExport]s.
import { DotNet } from './Microsoft.JSInterop.js';

let dispatcher;

export async function init(assemblyName) {
    if (dispatcher)
        return;

    const runtime = await globalThis.getDotnetRuntime(0);
    const assemblyExports = await runtime.getAssemblyExports(assemblyName);
    const dotnet = assemblyExports.Shiny.Infrastructure.UnoJSInterop;

    dispatcher = DotNet.attachDispatcher({
        beginInvokeDotNetFromJS: (callId, assembly, method, dotNetObjectId, argsJson) =>
            dotnet.BeginInvokeDotNet(callId ? callId.toString() : null, assembly, method, dotNetObjectId || 0, argsJson),

        // the dispatcher has already serialized [asyncHandle, succeeded, resultOrError] - the shape EndInvokeJS reads
        endInvokeJSFromDotNet: (asyncHandle, succeeded, serialized) => dotnet.EndInvokeJS(serialized),

        invokeDotNetFromJS: (assembly, method, dotNetObjectId, argsJson) =>
            dotnet.InvokeDotNet(assembly, method, dotNetObjectId || 0, argsJson),

        sendByteArray: (id, data) => dotnet.ReceiveByteArray(id, data)
    });

    // .NET disposes JS object references through globalThis.DotNet.disposeJSObjectReferenceById
    globalThis.DotNet = DotNet;
}

export function invokeJS(identifier, argsJson, resultType, targetInstanceId, callType) {
    return dispatcher.invokeJSFromDotNet(identifier, argsJson, resultType, targetInstanceId, callType);
}

export function beginInvokeJS(asyncHandle, identifier, argsJson, resultType, targetInstanceId, callType) {
    dispatcher.beginInvokeJSFromDotNet(asyncHandle, identifier, argsJson, resultType, targetInstanceId, callType);
}

export function endInvokeDotNet(callId, succeeded, resultJsonOrError) {
    dispatcher.endInvokeDotNetFromJS(callId, succeeded, resultJsonOrError);
}

export function receiveByteArray(id, data) {
    dispatcher.receiveByteArray(id, data);
}

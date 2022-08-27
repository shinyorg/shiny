using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;

namespace Shiny.Web.Infrastructure;


public class JsCallbackVoid
{
    readonly Subject<Unit> onResult = new();
    public IObservable<Unit> WhenResult() => this.onResult;

    public static DotNetObjectReference<JsCallbackVoid> CreateInterop()
        => DotNetObjectReference.Create(new JsCallbackVoid());


    [JSInvokable]
    public void Success() => this.onResult.OnNext(Unit.Default);

    [JSInvokable]
    public void Error(string error) => this.onResult.OnError(new Exception(error));
}


public class JsCallback<T>
{
    readonly Subject<T> onResult = new();
    public IObservable<T> WhenResult() => this.onResult;

    public static DotNetObjectReference<JsCallback<T>> CreateInterop()
        => DotNetObjectReference.Create(new JsCallback<T>());


    [JSInvokable]
    public void Success(T args) => this.onResult.OnNext(args);

    [JSInvokable]
    public void Error(string error) => this.onResult.OnError(new Exception(error));
}

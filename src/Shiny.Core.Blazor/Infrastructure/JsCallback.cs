using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;

namespace Shiny.Web.Infrastructure;


public class JsCallback<T>
{
    readonly Subject<T> onResult = new Subject<T>();
    public IObservable<T> WhenResult() => this.onResult;

    public static DotNetObjectReference<JsCallback<T>> CreateInterop()
        => DotNetObjectReference.Create(new JsCallback<T>());


    [JSInvokable]
    public void Success(T args) => this.onResult.OnNext(args);

    [JSInvokable]
    public void Error(string error) => this.onResult.OnError(new Exception(error));
}

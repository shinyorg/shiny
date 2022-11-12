using System;
using System.Globalization;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Shiny.Infrastructure;

namespace Shiny.SpeechRecognition;


public class SpeechRecognizer : ISpeechRecognizer, IShinyWebAssemblyService
{
    readonly Subject<Unit> speechEndSubj = new();
    readonly Subject<Unit> resultSubj = new();
    IJSInProcessObjectReference jsModule = null!;


    public async Task OnStart(IJSInProcessRuntime jsRuntime)
    {
        this.jsModule = await jsRuntime.ImportInProcess("Shiny.SpeechRecognition.Blazor", "speech.js");
    }


    public IObservable<string> ContinuousDictation(CultureInfo? culture = null) => this.Listen(culture, true, false);
    public IObservable<string> ListenUntilPause(CultureInfo? culture = null) => this.Listen(culture, false, false);


    IObservable<string> Listen(CultureInfo? culture, bool continuous, bool interim) => Observable
        .Create<string>(ob =>
        {
            var d = new CompositeDisposable();
            var objRef = DotNetObjectReference
                .Create(this)
                .DisposedBy(d);

            var lang = culture?.Name ?? "en-US";
            this.jsModule.InvokeVoid("startListener", lang, continuous, interim, objRef);

            this.resultSubj
                .Subscribe(x => ob.OnNext("TODO"))
                .DisposedBy(d);

            if (continuous)
            {
                this.speechEndSubj
                    .Subscribe(_ =>
                    {
                        ob.OnNext(null!);
                        ob.OnCompleted();
                    })
                    .DisposedBy(d);
            }
            return () =>
            {
                this.jsModule.InvokeVoid("stopListener");
                d.Dispose();
            };
        })
        .Where(x => x != null);


    public Task<AccessState> RequestAccess() => this.jsModule.RequestAccess("requestAccess");

    // TODO: translate result
    [JSInvokable] public void OnResult() => this.resultSubj.OnNext(Unit.Default);
    [JSInvokable] public void OnSpeechEnd() => this.speechEndSubj.OnNext(Unit.Default);


    public IObservable<bool> WhenListeningStatusChanged() => throw new NotImplementedException();
}


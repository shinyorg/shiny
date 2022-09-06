using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Shiny.Infrastructure;

namespace Shiny.Nfc.Blazor;


public class NfcManager : INfcManager, IShinyWebAssemblyService
{
    public Task OnStart(IJSInProcessRuntime jsRuntime) =>
        jsRuntime.ImportInProcess("Shiny.Nfc.Blazor", "nfc.js");

    public IObservable<AccessState> RequestAccess() => throw new NotImplementedException();
    public IObservable<INfcTag[]> WhenTagsDetected() => Observable.Create<INfcTag[]>(ob =>
    {
        //ob.

        return () =>
        {

        };
    });
}


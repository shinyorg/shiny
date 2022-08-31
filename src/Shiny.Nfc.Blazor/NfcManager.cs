using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Shiny.Infrastructure;

namespace Shiny.Nfc.Blazor;


public class NfcManager : INfcManager, IShinyWebAssemblyService
{
    public Task OnStart(IJSInProcessRuntime jsRuntime) => throw new NotImplementedException();
    public IObservable<AccessState> RequestAccess() => throw new NotImplementedException();
    public IObservable<INfcTag[]> WhenTagsDetected() => throw new NotImplementedException();
}


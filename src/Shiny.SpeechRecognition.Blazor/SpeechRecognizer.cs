using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Shiny.Infrastructure;

namespace Shiny.SpeechRecognition;


public class SpeechRecognizer : ISpeechRecognizer, IShinyWebAssemblyService
{
    readonly IJSInProcessRuntime jsProc;
    IJSInProcessObjectReference module = null!;


    public SpeechRecognizer(IJSRuntime jsRuntime)
    {
        this.jsProc = (IJSInProcessRuntime)jsRuntime;
    }


    public async Task OnStart()
    {
        this.module = await this.jsProc.ImportInProcess("Shiny.SpeechRecognition.Blazor", "speech.js");
    }


    public IObservable<string> ContinuousDictation(CultureInfo? culture = null) => throw new NotImplementedException();
    public IObservable<string> ListenUntilPause(CultureInfo? culture = null) => throw new NotImplementedException();
    public Task<AccessState> RequestAccess() => throw new NotImplementedException();
    
    public IObservable<bool> WhenListeningStatusChanged() => throw new NotImplementedException();
}


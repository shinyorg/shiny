using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Shiny.SpeechRecognition;


public class SpeechRecognizer : ISpeechRecognizer
{
    readonly IJSInProcessRuntime jsRef;


    public SpeechRecognizer(IJSRuntime jsRuntime)
    {
    }

    public IObservable<string> ContinuousDictation(CultureInfo? culture = null) => throw new NotImplementedException();
    public IObservable<string> ListenUntilPause(CultureInfo? culture = null) => throw new NotImplementedException();
    public Task<AccessState> RequestAccess() => throw new NotImplementedException();
    public IObservable<bool> WhenListeningStatusChanged() => throw new NotImplementedException();
}


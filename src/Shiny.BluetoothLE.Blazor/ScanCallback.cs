using System;
using System.Reactive.Subjects;
using Microsoft.JSInterop;

namespace Shiny.BluetoothLE;


public class ScanCallback : IDisposable
{
    readonly Subject<JsScanResult> subject = new();

    public IObservable<JsScanResult> WhenScanResult() => this.subject;

    [JSInvokable("OnScan")]
    public void OnScan(JsScanResult result)
        => this.subject.OnNext(result);

    public void Dispose()
        => this.subject.Dispose();
}

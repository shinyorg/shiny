using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Shiny.Infrastructure;
using Shiny.Web.Infrastructure;

namespace Shiny.BluetoothLE;


public class BleManager : IBleManager, IShinyWebAssemblyService
{
    IJSInProcessObjectReference jsModule = null!;
    public async Task OnStart(IJSInProcessRuntime jsRuntime)
        => this.jsModule = await jsRuntime.ImportInProcess("Shiny.BluetoothLE.Blazor", "ble.js");


    public bool IsScanning { get; private set; }

    public IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(string? serviceUuid = null) => throw new NotImplementedException();
    public IObservable<IPeripheral?> GetKnownPeripheral(string peripheralUuid) => throw new NotImplementedException();
    public void StopScan() => throw new NotImplementedException();

    public IObservable<AccessState> RequestAccess() => Observable.FromAsync(() => this.jsModule.RequestAccess());
    public IObservable<ScanResult> Scan(ScanConfig? config = null) => Observable.Create<ScanResult>(ob =>
    {
        var callback = JsCallback<JsScanResult>.CreateInterop();
        var disp = new CompositeDisposable(
            callback,
            callback
                .Value
                .WhenResult()
                .Subscribe(x => 
                {
                    var sr = new ScanResult(
                        new Peripheral(x.DeviceId, x.DeviceName ?? String.Empty),
                        x.Rssi,
                        new AdvertisementData(x)
                    );
                    ob.OnNext(sr);
                })
        );
        this.jsModule
            .InvokeVoidAsync("startScan", callback)
            .AsTask()
            .ContinueWith(x =>
            {
                if (x.Exception != null)
                    ob.OnError(x.Exception);
            });

        return () =>
        {
            disp.Dispose();
            this.jsModule.InvokeVoid("stopScan");
        };
    });
}
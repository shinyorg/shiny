using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Shiny.BluetoothLE;


public class BleManager : IBleManager, IAsyncDisposable
{
    readonly IJSRuntime jsRuntime;
    IJSObjectReference? jsModule;
    readonly Dictionary<string, Peripheral> peripherals = new();

    public BleManager(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
    }


    async Task<IJSObjectReference> GetModule()
    {
        this.jsModule ??= await this.jsRuntime
            .InvokeAsync<IJSObjectReference>("import", "./_content/Shiny.BluetoothLE.Blazor/ble.js")
            .ConfigureAwait(false);

        return this.jsModule;
    }


    public bool IsScanning { get; private set; }

    public AccessState CurrentAccess => AccessState.Unknown;

    public IPeripheral? GetKnownPeripheral(string peripheralUuid)
        => this.peripherals.Values.FirstOrDefault(x => x.Uuid.Equals(peripheralUuid, StringComparison.InvariantCultureIgnoreCase));

    public IEnumerable<IPeripheral> GetConnectedPeripherals() => Enumerable.Empty<IPeripheral>();

    public void StopScan()
    {
        this.IsScanning = false;
    }


    public IObservable<AccessState> RequestAccess() => Observable.FromAsync(async () =>
    {
        var module = await this.GetModule().ConfigureAwait(false);
        var result = await module.InvokeAsync<string>("requestAccess").ConfigureAwait(false);
        return result switch
        {
            "granted" => AccessState.Available,
            "denied" => AccessState.Denied,
            "prompt" => AccessState.Unknown,
            "notsupported" => AccessState.NotSupported,
            _ => AccessState.Unknown
        };
    });


    public IObservable<ScanResult> Scan(ScanConfig? scanConfig = null) => Observable.Create<ScanResult>(ob =>
    {
        if (this.IsScanning)
            throw new InvalidOperationException("There is already an existing scan");

        var callback = new ScanCallback();
        var dotNetRef = DotNetObjectReference.Create(callback);
        this.IsScanning = true;

        var disp = new CompositeDisposable(
            callback,
            dotNetRef,
            callback
                .WhenScanResult()
                .Subscribe(x =>
                {
                    if (!this.peripherals.TryGetValue(x.DeviceId, out var peripheral))
                    {
                        peripheral = new Peripheral(x.DeviceId, x.DeviceName);
                        this.peripherals[x.DeviceId] = peripheral;
                    }

                    var sr = new ScanResult(
                        peripheral,
                        x.Rssi,
                        new AdvertisementData(x)
                    );
                    ob.OnNext(sr);
                })
        );

        _ = Task.Run(async () =>
        {
            try
            {
                var module = await this.GetModule().ConfigureAwait(false);
                await module.InvokeVoidAsync("startScan", dotNetRef).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ob.OnError(ex);
            }
        });

        return () =>
        {
            this.IsScanning = false;
            disp.Dispose();
            _ = Task.Run(async () =>
            {
                try
                {
                    var module = await this.GetModule().ConfigureAwait(false);
                    await module.InvokeVoidAsync("stopScan").ConfigureAwait(false);
                }
                catch
                {
                    // best effort cleanup
                }
            });
        };
    });


    public async ValueTask DisposeAsync()
    {
        if (this.jsModule != null)
        {
            await this.jsModule.DisposeAsync().ConfigureAwait(false);
            this.jsModule = null;
        }
    }
}

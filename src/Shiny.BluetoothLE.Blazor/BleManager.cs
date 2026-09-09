using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Shiny.BluetoothLE;


/// <summary>
/// An <see cref="IBleManager"/> backed by Web Bluetooth, for Blazor WebAssembly.
/// </summary>
/// <remarks>
/// See <see cref="Scan"/> for the one behavioural difference from the native implementations: no
/// shipping browser enables free-running BLE scanning by default, so a chooser is used instead.
/// </remarks>
public class BleManager(IJSRuntime jsRuntime) : IBleManager, IAsyncDisposable
{
    IJSObjectReference? jsModule;
    readonly Dictionary<string, Peripheral> peripherals = new();


    async Task<IJSObjectReference> GetModule()
    {
        this.jsModule ??= await jsRuntime
            .InvokeAsync<IJSObjectReference>("import", "./_content/Shiny.BluetoothLE.Blazor/ble.js")
            .ConfigureAwait(false);

        return this.jsModule;
    }


    public bool IsScanning { get; private set; }

    public AccessState CurrentAccess => AccessState.Unknown;

    public IPeripheral? GetKnownPeripheral(string peripheralUuid)
        => this.peripherals.Values.FirstOrDefault(x => x.Uuid.Equals(peripheralUuid, StringComparison.InvariantCultureIgnoreCase));

    public IEnumerable<IPeripheral> GetConnectedPeripherals()
        => this.peripherals.Values.Where(x => x.Status == ConnectionState.Connected);

    public void StopScan() => this.IsScanning = false;


    /// <summary>
    /// Reports which Web Bluetooth discovery mechanisms this browser offers:
    /// <c>Advertisements</c> is free-running scanning via <c>requestLEScan</c> (Chromium only, and only
    /// behind <c>#enable-experimental-web-platform-features</c>); <c>Chooser</c> is <c>requestDevice</c>,
    /// which is always present. <see cref="Scan"/> picks between them automatically.
    /// </summary>
    public async Task<(bool Advertisements, bool Chooser)> GetScanSupport()
    {
        var module = await this.GetModule().ConfigureAwait(false);
        var result = await module.InvokeAsync<ScanSupport>("scanSupport").ConfigureAwait(false);
        return (result.Advertisements, result.Chooser);
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


    /// <summary>
    /// Discovers peripherals. Where the browser supports free-running scanning
    /// (<c>navigator.bluetooth.requestLEScan</c>) this behaves like the native implementations: a hot
    /// stream of advertisements until the subscription is disposed.
    /// </summary>
    /// <remarks>
    /// Otherwise it falls back to the Web Bluetooth chooser, which changes the semantics in ways callers
    /// must plan for:
    /// <list type="bullet">
    /// <item>It must be subscribed from a user gesture (a click handler), or the browser rejects it.</item>
    /// <item>It emits at most one peripheral - the one the user picked - and then completes.</item>
    /// <item>Dismissing the chooser completes the sequence without emitting.</item>
    /// <item><see cref="ScanResult.Rssi"/> is 0; the chooser reports no signal strength.</item>
    /// <item>
    /// Only services listed in <paramref name="scanConfig"/> are reachable afterwards. Web Bluetooth
    /// permits access solely to UUIDs declared up front, so pass every service you intend to use.
    /// </item>
    /// </list>
    /// </remarks>
    public IObservable<ScanResult> Scan(ScanConfig? scanConfig = null) => Observable.Create<ScanResult>(async ob =>
    {
        if (this.IsScanning)
            throw new InvalidOperationException("There is already an existing scan");

        var module = await this.GetModule().ConfigureAwait(false);
        var support = await module.InvokeAsync<ScanSupport>("scanSupport").ConfigureAwait(false);

        this.IsScanning = true;

        if (!support.Advertisements)
        {
            if (!support.Chooser)
            {
                this.IsScanning = false;
                throw new NotSupportedException("Web Bluetooth is not available in this browser.");
            }

            return await this.ScanWithChooser(module, scanConfig, ob).ConfigureAwait(false);
        }

        return this.ScanAdvertisements(module, ob);
    });


    async Task<IDisposable> ScanWithChooser(IJSObjectReference module, ScanConfig? scanConfig, IObserver<ScanResult> ob)
    {
        try
        {
            var uuids = scanConfig?.ServiceUuids ?? [];
            var result = await module.InvokeAsync<JsScanResult?>("requestDevice", uuids).ConfigureAwait(false);

            // null means the user dismissed the chooser - complete quietly rather than erroring.
            if (result is not null)
                ob.OnNext(this.ToScanResult(result));

            ob.OnCompleted();
        }
        catch (Exception ex)
        {
            ob.OnError(ex);
        }
        finally
        {
            this.IsScanning = false;
        }

        return Disposable.Empty;
    }


    IDisposable ScanAdvertisements(IJSObjectReference module, IObserver<ScanResult> ob)
    {
        var callback = new ScanCallback();
        var dotNetRef = DotNetObjectReference.Create(callback);

        var disp = new CompositeDisposable(
            callback,
            dotNetRef,
            callback
                .WhenScanResult()
                .Subscribe(x => ob.OnNext(this.ToScanResult(x)))
        );

        _ = Task.Run(async () =>
        {
            try
            {
                await module.InvokeVoidAsync("startScan", dotNetRef).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ob.OnError(ex);
            }
        });

        return new CompositeDisposable(disp, Disposable.Create(() =>
        {
            this.IsScanning = false;
            _ = Task.Run(async () =>
            {
                try
                {
                    await module.InvokeVoidAsync("stopScan").ConfigureAwait(false);
                }
                catch
                {
                    // best effort cleanup
                }
            });
        }));
    }


    ScanResult ToScanResult(JsScanResult result)
    {
        if (!this.peripherals.TryGetValue(result.DeviceId, out var peripheral))
        {
            peripheral = new Peripheral(this.GetModule, result.DeviceId, result.DeviceName);
            this.peripherals[result.DeviceId] = peripheral;
        }

        return new ScanResult(peripheral, result.Rssi, new AdvertisementData(result));
    }


    class ScanSupport
    {
        public bool Advertisements { get; set; }
        public bool Chooser { get; set; }
    }


    public async ValueTask DisposeAsync()
    {
        foreach (var peripheral in this.peripherals.Values)
            await peripheral.DisposeAsync().ConfigureAwait(false);

        this.peripherals.Clear();

        if (this.jsModule != null)
        {
            try
            {
                await this.jsModule.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
            }

            this.jsModule = null;
        }
    }
}

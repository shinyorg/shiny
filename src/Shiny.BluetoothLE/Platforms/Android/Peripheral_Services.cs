using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Android.Bluetooth;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE.Infrastructure;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    public IObservable<BleServiceInfo> GetService(string serviceUuid) => this.GetNativeService(serviceUuid).Select(this.FromNative);

    public IObservable<IReadOnlyList<BleServiceInfo>> GetServices()
    {
        // we want a fresh discovery everytime the user calls this
        // internal functions call GetNativeServices which doesn't force a refresh
        this.RequiresServiceDiscovery = true;
        return this
            .GetNativeServices()
            .Select(x => x.Select(this.FromNative).ToList());
    }
    

    Subject<GattStatus>? serviceDiscoverySubj;
    public override void OnServicesDiscovered(BluetoothGatt? gatt, GattStatus status)
    {
        // This peripheral is the callback for every GATT client it opens. A late OnServicesDiscovered from a client
        // already closed by a reconnect must not complete the discovery running on the current one: it "completed"
        // a fresh discovery within milliseconds of the refresh(), with the list still empty, and every service
        // lookup then threw "No service found" for the rest of the link.
        if (gatt != null && this.Gatt != null && !gatt.Equals(this.Gatt))
        {
            this.logger.LogDebug("Ignoring OnServicesDiscovered from a previous GATT client");
            return;
        }
        this.serviceDiscoverySubj?.OnNext(status);
    }


    // Service discovery is shared by every operation that needs a service - concurrent callers used to each run their
    // own refresh + DiscoverServices on the same GATT client. Callers own the timeout (Rx Timeout, a cancellation
    // token): once the last one waiting gives up, the discovery is torn down. A caller with no timeout of its own is
    // bounded by ServiceDiscoveryTimeout instead of waiting forever.
    static readonly TimeSpan ServiceDiscoveryTimeout = TimeSpan.FromSeconds(30);
    readonly SharedOperation<IReadOnlyList<BluetoothGattService>> serviceDiscovery = new();


    protected IObservable<IReadOnlyList<BluetoothGattService>> GetNativeServices() =>
        ObservableTask.FromAsync<IReadOnlyList<BluetoothGattService>>(async ct =>
        {
            this.AssertConnection();

            if (!this.RequiresServiceDiscovery && this.Gatt?.Services != null)
                return this.Gatt!.Services!.ToList();

            var disconnect = this.disconnectCts.Token;
            try
            {
                return await this.serviceDiscovery
                    .RunAsync(this.DiscoverServicesCore, ServiceDiscoveryTimeout, disconnect, ct)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (disconnect.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                // The discovery's token is linked to the disconnect token, so whether WaitForOperation turns the
                // cancellation into a BleException depends on the order the linked tokens fire in. Map it here
                // so a disconnect always reports as one, never as the caller cancelling.
                throw new BleException("[GetNativeServices] Peripheral disconnected during service discovery");
            }
        });


    async Task<IReadOnlyList<BluetoothGattService>> DiscoverServicesCore(CancellationToken ct)
    {
        this.AssertConnection();
        this.serviceDiscoverySubj ??= new();

        this.RefreshServices(); // force refresh of services on GATT
        var task = this.WaitForOperation(this.serviceDiscoverySubj, ct);
        if (!this.Gatt!.DiscoverServices())
            throw new InvalidOperationException("Android GATT reported that it could not run service discovery");

        var status = await task.ConfigureAwait(false);
        if (status != GattStatus.Success)
            throw new BleException("Service discovery failed with status: " + status);

        var services = this.Gatt.Services?.ToList() ?? new List<BluetoothGattService>();

        // An empty list after a "successful" discovery is not a real result (a GATT server always exposes at least
        // Generic Access/Attribute) - leave the peripheral flagged so the next operation discovers again instead of
        // serving the empty list as cached for the rest of the connection.
        if (services.Count > 0)
            this.RequiresServiceDiscovery = false;
        else
            this.logger.LogWarning("Service discovery reported success with no services - will rediscover on next use");

        return services;
    }


    protected IObservable<BluetoothGattService> GetNativeService(string serviceUuid) => this
        .GetNativeServices()
        .SelectMany(services =>
        {
            var native = this.Gatt!.GetService(Utils.ToUuidType(serviceUuid));
            if (native != null)
                return Observable.Return(native);

            // An EMPTY list flagged as discovered is stale (seen right after a reconnect): every lookup threw
            // "No service found" for the rest of the connection. Rediscover once. A non-empty list that lacks
            // the service is a real miss and fails straight away, so probing for optional services stays cheap.
            if (services.Count > 0)
                return Observable.Throw<BluetoothGattService>(new InvalidOperationException("No service found with uuid: " + serviceUuid));

            this.RequiresServiceDiscovery = true;
            return this
                .GetNativeServices()
                .Select(_ => this.Gatt!.GetService(Utils.ToUuidType(serviceUuid))
                    ?? throw new InvalidOperationException("No service found with uuid: " + serviceUuid));
        });


    public bool RequiresServiceDiscovery { get; private set; } = true;

    public void RefreshServices()
    {
        this.AssertConnection();
        // https://stackoverflow.com/questions/22596951/how-to-programmatically-force-bluetooth-low-energy-service-discovery-on-android
        try
        {
            var method = this.Gatt!.Class.GetMethod("refresh");
            if (method == null)
            {
                this.logger.LogWarning("No internal refresh method found");
            }
            else
            {
                var result = (bool)method.Invoke(this.Gatt);
                this.logger.LogInformation("Clear Internal Cache Refresh Result: " + result);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to clear internal device cache");
        }
    }


    Subject<Unit>? servChangeSubj;
    public IObservable<Unit> WhenServicesChanged() => this.servChangeSubj ??= new();
    public override void OnServiceChanged(BluetoothGatt gatt)
        => this.servChangeSubj?.OnNext(Unit.Default);


    protected BleServiceInfo FromNative(BluetoothGattService service) => new(service.Uuid.ToString());
}
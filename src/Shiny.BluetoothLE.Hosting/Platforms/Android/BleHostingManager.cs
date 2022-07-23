using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.OS;
using Java.Util;
using Shiny.BluetoothLE.Hosting.Internals;
using static Android.Manifest;
using Observable = System.Reactive.Linq.Observable;

namespace Shiny.BluetoothLE.Hosting;


public class BleHostingManager : IBleHostingManager
{
    readonly Dictionary<string, GattService> services = new();
    readonly GattServerContext context;
    AdvertisementCallbacks? adCallbacks;

    public BleHostingManager(AndroidPlatform platform) => this.context = new GattServerContext(platform);


    public async Task<AccessState> RequestAccess()
    {
        if (!this.context.Platform.IsMinApiLevel(23))
            return AccessState.NotSupported; //throw new InvalidOperationException("BLE Advertiser needs API Level 23+");

        var current = this.context.Manager.GetAccessState();
        if (current != AccessState.Available && current != AccessState.Unknown)
            return current;

        if (this.context.Platform.IsMinApiLevel(31))
        {
            var result = await this.context.Platform.RequestPermissions(Permission.BluetoothConnect, Permission.BluetoothAdvertise);
            if (!result.IsSuccess())
                return AccessState.Denied;
        }
        return AccessState.Available;
    }


    public bool IsAdvertising => this.adCallbacks != null;
    public IReadOnlyList<IGattService> Services => this.services.Values.Cast<IGattService>().ToArray();


    public IObservable<L2CapChannel> WhenL2CapChannelOpened(bool secure) => Observable.Create<L2CapChannel>(async ob =>
    {
        (await this.RequestAccess()).Assert();

        var ct = new CancellationTokenSource();
        var ad = BluetoothAdapter.DefaultAdapter;
        if (ad == null)
            throw new InvalidOperationException("No Bluetooth Adaptor found");

        var serverSocket = secure
            ? ad.ListenUsingL2capChannel()
            : ad.ListenUsingInsecureL2capChannel();

        _ = Task.Run(() =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var socket = serverSocket.Accept(30000);
                    if (socket != null && !ct.IsCancellationRequested)
                    {
                        ob.OnNext(new L2CapChannel(
                            (ushort)serverSocket.Psm,
                            socket.InputStream!,
                            socket.OutputStream!
                        ));
                    }
                }
                catch { }
            }
        });

        return () =>
        {
            ct.Cancel();
            serverSocket?.Dispose();
        };
    });


    public async Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder)
    {
        (await this.RequestAccess()).Assert();

        var service = new GattService(this.context, uuid, primary);
        serviceBuilder(service);

        //var task = this.context
        //    .WhenServiceAdded
        //    .Take(1)
        //    .Timeout(TimeSpan.FromSeconds(5))
        //    .ToTask();

        if (!this.context.Server.AddService(service.Native))
            throw new InvalidOperationException("Service operation did not complete - look at logs");

        //await task.ConfigureAwait(false);
        this.services.Add(uuid, service);
        return service;
    }


    public void ClearServices()
    {
        this.services.Clear();
        this.context.Server.ClearServices();
        this.Cleanup();
    }


    public void RemoveService(string serviceUuid)
    {
        var uuid = UUID.FromString(serviceUuid);
        var s = this.services.ContainsKey(serviceUuid)
            ? this.services[serviceUuid].Native
            : this.context.Server.Services?.FirstOrDefault(x => x.Uuid?.Equals(uuid) ?? false);

        if (s != null)
            this.context.Server.RemoveService(s);

        this.services.Remove(serviceUuid);
        if (this.services.Count == 0)
            this.Cleanup();
    }


    public async Task StartAdvertising(AdvertisementOptions? options = null)
    {
        (await this.RequestAccess()).Assert();

        options ??= new AdvertisementOptions();
        var tcs = new TaskCompletionSource<bool>();
        this.adCallbacks = new AdvertisementCallbacks(
            () => tcs.SetResult(true),
            ex => tcs.SetException(ex)
        );

        var settings = new AdvertiseSettings.Builder()
            .SetAdvertiseMode(AdvertiseMode.Balanced)
            .SetConnectable(true);

        var data = new AdvertiseData.Builder()
            // TODO: kill this in favour of local name below
            .SetIncludeDeviceName(options.AndroidIncludeDeviceName)
            .SetIncludeTxPowerLevel(options.AndroidIncludeTxPower);

        if (options.ManufacturerData != null)
            data = data.AddManufacturerData(options.ManufacturerData.CompanyId, options.ManufacturerData.Data);

        if (options.LocalName != null)
            BluetoothAdapter.DefaultAdapter.SetName(options.LocalName); // TODO: verify name length with exception

        var serviceUuids = options.UseGattServiceUuids
            ? this.services.Keys.ToList()
            : options.ServiceUuids;

        foreach (var uuid in serviceUuids)
        {
            var nativeUuid = UUID.FromString(uuid);
            var parcel = new ParcelUuid(nativeUuid);
            data = data.AddServiceUuid(parcel);
        }

        this.context
            .Manager
            .Adapter
            .BluetoothLeAdvertiser
            .StartAdvertising(
                settings.Build(),
                data.Build(),
                this.adCallbacks
            );

        await tcs.Task;
    }


    public void StopAdvertising()
    {
        this.context
            .Manager
            .Adapter
            .BluetoothLeAdvertiser
            .StopAdvertising(this.adCallbacks);
        this.adCallbacks = null;
    }


    void Cleanup()
    {
        foreach (var service in this.services)
            service.Value.Dispose();

        this.services.Clear();
        this.context.CloseServer();
    }
}
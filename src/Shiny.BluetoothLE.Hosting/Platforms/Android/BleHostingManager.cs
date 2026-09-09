using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Bluetooth.LE;
using Android.OS;
using Java.Util;
using Shiny.BluetoothLE.Hosting.Internals;
using static Android.Manifest;

namespace Shiny.BluetoothLE.Hosting;


public partial class BleHostingManager(AndroidPlatform platform) : IBleHostingManager
{
    readonly Dictionary<string, GattService> services = new();
    readonly GattServerContext context = new(platform);
    AdvertisementCallbacks? adCallbacks;


    public AccessState AdvertisingAccessStatus
    {
        get
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(23))
                return AccessState.NotSupported;

            var status = AccessState.Available;
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
                status = this.context.Platform.GetCurrentPermissionStatus(Permission.BluetoothAdvertise);

            if (status == AccessState.Available)
                status = this.context.Manager.GetAccessState();

            return status;
        }
    }


    public AccessState GattAccessStatus
    {
        get
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(23))
                return AccessState.NotSupported;

            var status = AccessState.Available;
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
                status = this.context.Platform.GetCurrentPermissionStatus(Permission.BluetoothConnect);

            if (status == AccessState.Available)
                status = this.context.Manager.GetAccessState();

            return status;
        }
    }


    public async Task<AccessState> RequestAccess(bool advertise = true, bool connect = true)
    {
        if (!advertise && !connect)
            throw new ArgumentException("You must request at least 1 permission");

        if (!OperatingSystem.IsAndroidVersionAtLeast(23))
            return AccessState.NotSupported; //throw new InvalidOperationException("BLE Advertiser needs API Level 23+");

        var current = this.context.Manager.GetAccessState();
        if (current != AccessState.Available && current != AccessState.Unknown)
            return current;

        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            var perms = new List<string>();
            if (advertise)
                perms.Add(Permission.BluetoothAdvertise);

            if (connect)
                perms.Add(Permission.BluetoothConnect);

            var result = await this.context.Platform.RequestPermissions(perms.ToArray());
            if (!result.IsSuccess())
                return AccessState.Denied;
        }
        return AccessState.Available;
    }


    public bool IsAdvertising => this.adCallbacks != null;
    public IReadOnlyList<IGattService> Services => this.services.Values.Cast<IGattService>().ToArray();

    public async Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder)
    {
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
        if (this.IsAdvertising)
            throw new InvalidOperationException("Advertisement is already running");
        
        options ??= new();

        var settings = new AdvertiseSettings.Builder()!
            .SetAdvertiseMode(AdvertiseMode.Balanced)!
            .SetConnectable(options.IsConnectable)!;

        var data = new AdvertiseData.Builder()!
            .SetIncludeTxPowerLevel(options.IncludeTxPower)!;

        foreach (var uuid in options.ServiceUuids)
        {
            var nativeUuid = UUID.FromString(uuid);
            var parcel = new ParcelUuid(nativeUuid);
            data = data!.AddServiceUuid(parcel);
        }

        foreach (var sd in options.ServiceData)
        {
            var parcel = new ParcelUuid(UUID.FromString(sd.Uuid));
            data = data!.AddServiceData(parcel, sd.Data);
        }

        if (options.ManufacturerData != null)
            data = data!.AddManufacturerData(options.ManufacturerData.CompanyId, options.ManufacturerData.Data);

        await this.DoAdvertise(settings, data);

        if (options.LocalName != null)
        {
            // TODO: verify name length with exception
            this.context
                .Platform
                .GetBluetoothAdapter()!
                .SetName(options.LocalName);
        }
    }


    public void StopAdvertising()
    {
        if (!this.IsAdvertising)
            return;
        
        var adapter = this.context.Platform.GetBluetoothAdapter()!;
        adapter.BluetoothLeAdvertiser!.StopAdvertising(this.adCallbacks);
        this.adCallbacks = null;
    }


    public async Task<L2CapInstance> OpenL2Cap(bool secure, Action<L2CapChannel> onOpen)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
            throw new InvalidOperationException("L2Cap hosting requires Android API 29+");

        (await this.RequestAccess()).Assert();

        var ad = this.context.Platform.GetBluetoothAdapter()
            ?? throw new InvalidOperationException("No Bluetooth adapter available");

        var serverSocket = secure
            ? ad.ListenUsingL2capChannel()
            : ad.ListenUsingInsecureL2capChannel();

        var psm = Convert.ToUInt16(serverSocket!.Psm);
        var cts = new CancellationTokenSource();

        _ = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var socket = serverSocket.Accept();
                    if (socket == null || cts.IsCancellationRequested)
                    {
                        socket?.Dispose();
                        return;
                    }

                    onOpen(new L2CapChannel(
                        psm,
                        socket.RemoteDevice?.Address ?? string.Empty,
                        data => System.Reactive.Linq.Observable.FromAsync(ct => socket.OutputStream!.WriteAsync(data, 0, data.Length, ct)),
                        socket.ListenForData(),
                        () => socket.Dispose()
                    ));
                }
                catch when (cts.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error accepting L2Cap connection on PSM {psm}: {ex}");
                }
            }
        });

        return new L2CapInstance(
            psm,
            () =>
            {
                cts.Cancel();
                serverSocket.Dispose();
            }
        );
    }

    public Task AdvertiseBeacon(Guid uuid, ushort major, ushort minor, sbyte? txpower = null)
    {
        var settings = new AdvertiseSettings.Builder()!
            .SetAdvertiseMode(AdvertiseMode.LowPower)!
            .SetTimeout(0)!
            .SetTxPowerLevel(AdvertiseTx.PowerMedium)!
            .SetConnectable(false)!;

        var data = new AdvertiseData.Builder()!;

        // Every multi-byte field in an iBeacon payload is big-endian. This used to build the packet
        // from Guid.ToByteArray() and BitConverter.GetBytes(), both of which are little-endian here,
        // so the UUID's first three fields and both the major and minor went out byte-swapped and no
        // receiver could match the beacon. It also prefixed 0xBE 0xAC - AltBeacon's identifier -
        // where iBeacon wants 0x02 0x15.
        var bytes = IBeaconPacket.Build(uuid, major, minor, txpower ?? IBeaconPacket.DefaultTxPower);
        data.AddManufacturerData(IBeaconPacket.AppleCompanyId, bytes);

        return this.DoAdvertise(settings, data);
    }


    void Cleanup()
    {
        foreach (var service in this.services)
            service.Value.Dispose();

        this.services.Clear();
        this.context.CloseServer();
    }


    async Task DoAdvertise(AdvertiseSettings.Builder settings, AdvertiseData.Builder data)
    {
        var tcs = new TaskCompletionSource<bool>();
        this.adCallbacks = new AdvertisementCallbacks(
            () => tcs.SetResult(true),
            ex => tcs.SetException(ex)
        );
        this.context
            .Platform
            .GetBluetoothAdapter()!
            .BluetoothLeAdvertiser!
            .StartAdvertising(
                settings.Build(),
                data.Build(),
                this.adCallbacks
            );

        await tcs.Task.ConfigureAwait(false);
    }
}
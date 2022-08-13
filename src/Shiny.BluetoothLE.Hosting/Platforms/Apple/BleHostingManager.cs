using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using CoreBluetooth;
using Foundation;

namespace Shiny.BluetoothLE.Hosting;


public partial class BleHostingManager : IBleHostingManager
{
    CBPeripheralManager _manager;
    protected CBPeripheralManager Manager
    {
        get
        {
            this._manager ??= new();
            return this._manager;
        }
    }


    readonly Dictionary<string, GattService> services = new();


    async Task<ushort> PublishL2Cap(bool secure)
    {
        var tcs = new TaskCompletionSource<ushort>();

        var handler = new EventHandler<CBPeripheralManagerL2CapChannelOperationEventArgs>((sender, args) =>
        {
            if (args.Error == null)
            {
                tcs.TrySetResult(args.Psm);
            }
            else
            {
                tcs.TrySetException(new InvalidOperationException(args.Error.Description));
            }
        });
        this.Manager.DidPublishL2CapChannel += handler;

        try
        {
            this.Manager.PublishL2CapChannel(secure);
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            this.Manager.DidPublishL2CapChannel -= handler;
        }
    }


    public async Task<L2CapInstance> OpenL2Cap(bool secure, Action<L2CapChannel> onOpen)
    {
        (await this.RequestAccess(false, true)).Assert();

        var handler = new EventHandler<CBPeripheralManagerOpenL2CapChannelEventArgs>((sender, args) =>
        {
            onOpen(new L2CapChannel(
                args.Channel!.Psm,
                args.Channel!.InputStream.ToStream(),
                args.Channel!.OutputStream.ToStream()
            ));
        });
        this.Manager.DidOpenL2CapChannel += handler;
        var psm = await this.PublishL2Cap(secure);
        return new L2CapInstance(
            psm,
            () =>
            {
                this.Manager.UnpublishL2CapChannel(psm);
                this.Manager.DidOpenL2CapChannel -= handler;
            }
        );
    }


    public bool IsAdvertising => this.Manager.Advertising;
    public async Task StartAdvertising(AdvertisementOptions? options = null)
    {
        (await this.RequestAccess(true, false)).Assert();

        if (this.Manager.Advertising)
            throw new InvalidOperationException("Advertising is already active");

        options ??= new AdvertisementOptions();
        var tcs = new TaskCompletionSource<bool>();
        var handler = new EventHandler<NSErrorEventArgs>((sender, args) =>
        {
            if (args.Error == null)
                tcs.SetResult(true);
            else
                tcs.SetException(new ArgumentException(args.Error.LocalizedDescription));
        });

        try
        {
            this.Manager.AdvertisingStarted += handler;

            var opts = new StartAdvertisingOptions();
            if (options.LocalName != null)
                opts.LocalName = options.LocalName;

            if (options.ServiceUuids.Length > 0)
            {
                opts.ServicesUUID = options
                    .ServiceUuids
                    .Select(CBUUID.FromString)
                    .ToArray();
            }

            this.Manager.StartAdvertising(opts);
            await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            this.Manager.AdvertisingStarted -= handler;
        }
    }


    public void StopAdvertising() => this.Manager.StopAdvertising();


    public async Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder)
    {
        // TODO: not sure about this
        //if (AppleExtensions.HasPlistValue("NSBluetoothAlwaysUsageDescription", 13) && !primary)
        //    throw new InvalidOperationException("You must specify your service as primary when using bg BLE");
        (await this.RequestAccess(false, true)).Assert();

        var service = new GattService(this.Manager, uuid, primary);
        serviceBuilder(service);

        var tcs = new TaskCompletionSource<bool>();
        var handler = new EventHandler<CBPeripheralManagerServiceEventArgs>((sender, args) =>
        {
            if (args.Service.UUID != service.Native.UUID)
                return;

            if (args.Error == null)
                tcs.TrySetResult(true);
            else
                tcs.SetException(new InvalidOperationException("Could not add BLE service - " + args.Error));
        });

        try
        {
            this.Manager.ServiceAdded += handler;
            this.Manager.AddService(service.Native);
            await tcs.Task.ConfigureAwait(false);

            this.services.Add(uuid, service);
        }
        finally
        {
            this.Manager.ServiceAdded -= handler;
        }
        return service;
    }


    public void RemoveService(string serviceUuid)
    {
        if (!this.services.ContainsKey(serviceUuid))
        {
            var native = new CBMutableService(CBUUID.FromString(serviceUuid), false);
            this.Manager.RemoveService(native); // let's try to remove anyhow
        }
        else
        {
            var service = this.services[serviceUuid];
            this.Manager.RemoveService(service.Native);
            service.Dispose();
            this.services.Remove(serviceUuid);
        }
    }


    public void ClearServices()
    {
        foreach (var service in this.services.Values)
        {
            this.Manager.RemoveService(service.Native);
            service.Dispose();
        }
        this.services.Clear();
        this.Manager.RemoveAllServices();
    }


    public IReadOnlyList<IGattService> Services => this.services.Values.Cast<IGattService>().ToList();


    public async Task<AccessState> RequestAccess(bool advertise = true, bool connect = true)
    {
        var status = ToStatus(this.Manager.State);
        if (status == AccessState.Unknown)
        {
            var tcs = new TaskCompletionSource<bool>();
            var handler = new EventHandler((sender, args) =>
            {
                status = ToStatus(this.Manager.State);
                if (status != AccessState.Unknown)
                    tcs.TrySetResult(true);
            });
            // this should not hang...
            this.Manager.StateUpdated += handler;
            await tcs.Task.ConfigureAwait(false);
            this.Manager.StateUpdated -= handler;
        }
        return status;
    }

#if XAMARIN
    static AccessState ToStatus(CBPeripheralManagerState state) => state switch
    {
        CBPeripheralManagerState.PoweredOff => AccessState.Disabled,
        CBPeripheralManagerState.Unauthorized => AccessState.Denied,
        CBPeripheralManagerState.Unsupported => AccessState.NotSupported,
        CBPeripheralManagerState.PoweredOn => AccessState.Available,
        //  CBPeripheralManagerState.Resetting, Unknown
        _ => AccessState.Unknown
    };

#else
    static AccessState ToStatus(CBManagerState state) => state switch
    {
        CBManagerState.PoweredOff => AccessState.Disabled,
        CBManagerState.Unauthorized => AccessState.Denied,
        CBManagerState.Unsupported => AccessState.NotSupported,
        CBManagerState.PoweredOn => AccessState.Available,
        //  CBPeripheralManagerState.Resetting, Unknown
        _ => AccessState.Unknown
    };
#endif
}
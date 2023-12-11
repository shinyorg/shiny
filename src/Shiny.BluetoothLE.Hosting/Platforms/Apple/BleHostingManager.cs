using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Foundation;
using CoreBluetooth;
using CoreLocation;

namespace Shiny.BluetoothLE.Hosting;


public partial class BleHostingManager : IBleHostingManager
{
    CBPeripheralManager manager;
    protected CBPeripheralManager Manager
    {
        get
        {
            this.manager ??= new();
            return this.manager;
        }
    }

    public AccessState AdvertisingAccessStatus => CBPeripheralManager.Authorization switch
    {
        CBManagerAuthorization.NotDetermined => AccessState.Unknown,
        CBManagerAuthorization.Denied => AccessState.Denied,
        //CBManagerAuthorization.Restricted => ToStatus(this.Manager.State),
        CBManagerAuthorization.AllowedAlways => ToStatus(this.Manager.State)
    };

    // only one state on iOS/Mac
    public AccessState GattAccessStatus => this.AdvertisingAccessStatus;

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


    public bool IsAdvertising => this.Manager.Advertising;
    public Task StartAdvertising(AdvertisementOptions? options = null)
    {
        options ??= new AdvertisementOptions();
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
        return this.DoAdvertise(opts.Dictionary);
    }


    public void StopAdvertising() => this.Manager.StopAdvertising();

    //public async Task<L2CapInstance> OpenL2Cap(bool secure, Action<L2CapChannel> onOpen)
    //{
    //    (await this.RequestAccess(false, true)).Assert();

    //    var handler = new EventHandler<CBPeripheralManagerOpenL2CapChannelEventArgs>((sender, args) =>
    //    {
    //        //args.Channel.InputStream.Status == NSStreamStatus.Open
    //        var c = args.Channel!;
    //        c.InputStream.Open();
    //        c.OutputStream.Open();

    //        onOpen(new L2CapChannel(
    //            c.Psm,
    //            c.Peer.Identifier.ToString(),
    //            data => Observable.FromAsync(ct => c.OutputStream.WriteAsync(data, 0, data.Length, ct)),
    //            c.InputStream.ListenForData()
    //        ));
    //    });
    //    this.Manager.DidOpenL2CapChannel += handler;
    //    var psm = await this.PublishL2Cap(secure);
    //    return new L2CapInstance(
    //        psm,
    //        () =>
    //        {
    //            this.Manager.UnpublishL2CapChannel(psm);
    //            this.Manager.DidOpenL2CapChannel -= handler;
    //        }
    //    );
    //}


    public Task AdvertiseBeacon(Guid uuid, ushort major, ushort minor, sbyte? txpower = null)
    {
        NSMutableDictionary data = null!;
#if MACCATALYST
            // throw new NotSupportedException("Not supported on MacCatalyst");
            var bytes = new List<byte>();
            bytes.AddRange(uuid.ToByteArray());
            bytes.AddRange(BitConverter.GetBytes(major));
            bytes.AddRange(BitConverter.GetBytes(minor));
            bytes.Add((byte)(txpower ?? -75));

            data = new NSMutableDictionary();
            data.SetValueForKey(NSData.FromArray(bytes.ToArray()), new NSString("kCBAdvDataAppleBeaconKey"));
#else
        var id = new CLBeaconIdentityConstraint(uuid.ToNSUuid(), major, minor);
        var beacon = new CLBeaconRegion(id, "ShinyBle");
        NSNumber? number = null;
        if (txpower != null)
            number = new NSNumber(txpower.Value);
        //	data	{{     kCBAdvDataAppleBeaconKey = {length = 21, bytes = 0x14fdbc1ba0d1441abd822bd8b49c0ba5000b00deff}; }}	Foundation.NSMutableDictionary
        data = beacon.GetPeripheralData(number); // this returns null on catalyst
#endif
        return this.DoAdvertise(data);
    }


    public async Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder)
    {
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


    async Task DoAdvertise(NSDictionary parameters)
    {
        (await this.RequestAccess(true, false)).Assert();

        if (this.Manager.Advertising)
            throw new InvalidOperationException("Advertising is already active");
        
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
            this.Manager.StartAdvertising(parameters);
            await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            this.Manager.AdvertisingStarted -= handler;
        }
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
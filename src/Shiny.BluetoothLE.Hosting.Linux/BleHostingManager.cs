using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE.Hosting.Bluez;
using Tmds.DBus.Protocol;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// BLE peripheral hosting via BlueZ on Linux. Talks to the system D-Bus and exposes
/// services/characteristics/advertisements through BlueZ's GattManager1 / LEAdvertisingManager1.
/// </summary>
public class BleHostingManager : IBleHostingManager, IAsyncDisposable
{
    readonly ILogger<BleHostingManager> logger;
    readonly Dictionary<string, GattService> services = new();
    DBusConnection? connection;
    bool advertising;


    public BleHostingManager(ILogger<BleHostingManager> logger)
    {
        this.logger = logger;
    }


    public AccessState AdvertisingAccessStatus { get; private set; } = AccessState.Unknown;
    public AccessState GattAccessStatus { get; private set; } = AccessState.Unknown;
    public bool IsAdvertising => this.advertising;
    public IReadOnlyList<IGattService> Services => this.services.Values.Cast<IGattService>().ToList();


    public async Task<AccessState> RequestAccess(bool advertise = true, bool connect = true)
    {
        try
        {
            await this.EnsureConnectionAsync().ConfigureAwait(false);

            // Probe BlueZ adapter for Powered=true. If the adapter object exists and is reachable,
            // we treat that as "Available". A more thorough probe would call Properties.Get on
            // org.bluez.Adapter1 / Powered.
            var available = await this.IsAdapterAvailableAsync().ConfigureAwait(false)
                ? AccessState.Available
                : AccessState.Disabled;

            if (advertise) this.AdvertisingAccessStatus = available;
            if (connect)   this.GattAccessStatus = available;
            return available;
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "BlueZ not reachable on system D-Bus");
            this.AdvertisingAccessStatus = AccessState.NotSupported;
            this.GattAccessStatus = AccessState.NotSupported;
            return AccessState.NotSupported;
        }
    }


    public async Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder)
    {
        await this.EnsureConnectionAsync().ConfigureAwait(false);

        var svc = new GattService(uuid, primary);
        serviceBuilder(svc);

        // Assign object paths now so user code (e.g. Notify) can reason about them.
        var index = this.services.Count;
        svc.ObjectPath = $"{BluezConstants.ApplicationRootPath}/service{index}";
        for (var i = 0; i < svc.NativeCharacteristics.Count; i++)
            svc.NativeCharacteristics[i].ObjectPath = $"{svc.ObjectPath}/char{i}";

        this.services.Add(uuid, svc);

        // TODO: export GattService1/GattCharacteristic1 D-Bus objects via DBusConnection.AddMethodHandler
        // and (re)call org.bluez.GattManager1.RegisterApplication on the adapter so BlueZ picks
        // up the application root at BluezConstants.ApplicationRootPath.
        throw new NotSupportedException(
            "GATT server registration with BlueZ is not yet implemented. " +
            "The service has been recorded but not exported over D-Bus."
        );
    }


    public void RemoveService(string serviceUuid)
    {
        if (this.services.Remove(serviceUuid))
        {
            // TODO: unregister application from BlueZ if no services remain, otherwise re-register.
        }
    }


    public void ClearServices()
    {
        this.services.Clear();
        // TODO: call GattManager1.UnregisterApplication
    }


    public Task StartAdvertising(AdvertisementOptions? options = null)
    {
        // TODO: export LEAdvertisement1 object at BluezConstants.AdvertisementPath with
        // Type=peripheral, ServiceUUIDs, LocalName from options, then call
        // org.bluez.LEAdvertisingManager1.RegisterAdvertisement on the adapter.
        throw new NotSupportedException("LE advertising via BlueZ is not yet implemented.");
    }


    public void StopAdvertising()
    {
        if (!this.advertising) return;
        // TODO: call LEAdvertisingManager1.UnregisterAdvertisement and remove the exported object.
        this.advertising = false;
    }


    public Task AdvertiseBeacon(Guid uuid, ushort major, ushort minor, sbyte? txpower = null)
        => throw new NotSupportedException("iBeacon advertising is not supported on Linux/BlueZ.");


    public Task<L2CapInstance> OpenL2Cap(bool secure, Action<L2CapChannel> onOpen)
    {
        // L2CAP CoC is independent of BlueZ's GATT/advertising surface — it goes straight
        // to the kernel via AF_BLUETOOTH sockets, so this works even though our GATT server
        // and advertising hooks are still stubs.
        var (listener, psm) = L2CapSocket.Listen(secure);
        var cts = new CancellationTokenSource();

        _ = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                L2CapSocket.L2CapHandle? client = null;
                string? peer = null;
                try
                {
                    var accepted = L2CapSocket.Accept(listener);
                    if (accepted == null || cts.IsCancellationRequested)
                    {
                        accepted?.Client.Dispose();
                        return;
                    }
                    (client, peer) = (accepted.Value.Client, accepted.Value.PeerAddress);
                }
                catch (Exception ex) when (!cts.IsCancellationRequested)
                {
                    this.logger.LogWarning(ex, "L2CAP accept failed on PSM {Psm}", psm);
                    continue;
                }

                var connectionCts = new CancellationTokenSource();
                var rxSubj = new System.Reactive.Subjects.Subject<byte[]>();
                var connected = client!;

                _ = Task.Run(() =>
                {
                    try
                    {
                        while (!connectionCts.IsCancellationRequested)
                        {
                            var frame = connected.Receive();
                            if (frame == null) break;
                            rxSubj.OnNext(frame);
                        }
                        rxSubj.OnCompleted();
                    }
                    catch (Exception ex)
                    {
                        rxSubj.OnError(ex);
                    }
                });

                onOpen(new L2CapChannel(
                    psm,
                    peer!,
                    data => System.Reactive.Linq.Observable.FromAsync(ct => connected.SendAsync(data, ct)),
                    rxSubj,
                    () =>
                    {
                        connectionCts.Cancel();
                        connected.Dispose();
                    }
                ));
            }
        });

        return Task.FromResult(new L2CapInstance(
            psm,
            () =>
            {
                cts.Cancel();
                listener.Dispose();
            }
        ));
    }


    async Task EnsureConnectionAsync(CancellationToken ct = default)
    {
        if (this.connection != null) return;
        this.connection = new DBusConnection(DBusAddress.System!);
        await this.connection.ConnectAsync().ConfigureAwait(false);
    }


    async Task<bool> IsAdapterAvailableAsync()
    {
        if (this.connection == null)
            return false;

        // Best-effort: ask BlueZ Properties.Get for Adapter1.Powered. If the call succeeds and
        // returns true, the adapter is up. We swallow exceptions so missing/down adapters return
        // false rather than throwing.
        try
        {
            var msg = this.connection.CreateGetPropertyCall(
                BluezConstants.Service,
                BluezConstants.DefaultAdapterPath,
                BluezConstants.AdapterInterface,
                "Powered"
            );

            var powered = await this.connection.CallMethodAsync(
                msg,
                static (Message reply, object? _) =>
                {
                    var reader = reply.GetBodyReader();
                    return reader.ReadBoolVariant();
                }
            ).ConfigureAwait(false);

            return powered;
        }
        catch
        {
            return false;
        }
    }


    public ValueTask DisposeAsync()
    {
        this.connection?.Dispose();
        this.connection = null;
        return ValueTask.CompletedTask;
    }
}

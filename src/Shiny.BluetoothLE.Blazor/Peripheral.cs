using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Shiny.BluetoothLE;


/// <summary>
/// An <see cref="IPeripheral"/> backed by the Web Bluetooth GATT API.
/// </summary>
/// <remarks>
/// Two platform constraints are worth knowing before using this:
/// <list type="bullet">
/// <item>
/// A GATT service is only reachable if its UUID was declared when the device was chosen (as a scan
/// filter or in <c>optionalServices</c>). Web Bluetooth has no post-hoc "discover everything", so
/// <see cref="GetServices"/> returns only what was declared - see <see cref="BleManager.Scan"/>.
/// </item>
/// <item>
/// <see cref="ReadRssi"/> and <see cref="WhenServicesChanged"/> have no Web Bluetooth equivalent and
/// throw <see cref="NotSupportedException"/>. RSSI is observable during a scan, not on a live connection.
/// </item>
/// </list>
/// </remarks>
public class Peripheral : IPeripheral, IAsyncDisposable
{
    readonly Func<Task<IJSObjectReference>> getModule;
    readonly BehaviorSubject<ConnectionState> status = new(ConnectionState.Disconnected);
    readonly Subject<BleException> connectionFailed = new();
    readonly PeripheralCallback callback = new();
    readonly DotNetObjectReference<PeripheralCallback> callbackRef;
    readonly IDisposable disconnectSub;


    public Peripheral(Func<Task<IJSObjectReference>> moduleAccessor, string uuid, string? name)
    {
        this.getModule = moduleAccessor ?? throw new ArgumentNullException(nameof(moduleAccessor));
        this.Uuid = uuid;
        this.Name = name;
        this.callbackRef = DotNetObjectReference.Create(this.callback);

        this.disconnectSub = this.callback
            .WhenConnectionStateChanged()
            .Subscribe(connected =>
            {
                var next = connected ? ConnectionState.Connected : ConnectionState.Disconnected;
                if (this.status.Value != next)
                    this.status.OnNext(next);
            });
    }


    public string? Name { get; }
    public string Uuid { get; }

    public ConnectionState Status => this.status.Value;


    /// <summary>
    /// Web Bluetooth never exposes the negotiated ATT MTU. The browser fragments transparently and caps
    /// a single write at the spec limit of 512 bytes, so that ceiling is reported here. Peripherals with
    /// small receive buffers may still need the caller to write in smaller chunks.
    /// </summary>
    public int Mtu => 512;


    public void Connect(ConnectionConfig? config = null)
    {
        if (this.status.Value is ConnectionState.Connected or ConnectionState.Connecting)
            return;

        this.status.OnNext(ConnectionState.Connecting);

        _ = Task.Run(async () =>
        {
            try
            {
                var module = await this.getModule().ConfigureAwait(false);
                await module.InvokeVoidAsync("connect", this.Uuid, this.callbackRef).ConfigureAwait(false);
                this.status.OnNext(ConnectionState.Connected);
            }
            catch (Exception ex)
            {
                this.status.OnNext(ConnectionState.Disconnected);
                this.connectionFailed.OnNext(new BleException($"Failed to connect to '{this.Uuid}'", ex));
            }
        });
    }


    public void CancelConnection()
    {
        if (this.status.Value == ConnectionState.Disconnected)
            return;

        this.status.OnNext(ConnectionState.Disconnecting);

        _ = Task.Run(async () =>
        {
            try
            {
                var module = await this.getModule().ConfigureAwait(false);
                await module.InvokeVoidAsync("disconnect", this.Uuid).ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
                // Host is gone; the browser has torn the link down with it.
            }
            finally
            {
                this.status.OnNext(ConnectionState.Disconnected);
            }
        });
    }


    public IObservable<ConnectionState> WhenStatusChanged() => this.status;

    public IObservable<BleException> WhenConnectionFailed() => this.connectionFailed;


    public IObservable<Unit> WhenServicesChanged() => Observable.Throw<Unit>(
        new NotSupportedException("Web Bluetooth does not raise GATT service-change events.")
    );


    public IObservable<int> ReadRssi() => Observable.Throw<int>(
        new NotSupportedException("Web Bluetooth exposes RSSI only on scan advertisements, not on a connected peripheral.")
    );


    // ---- services / characteristics --------------------------------------------------------------

    public IObservable<IReadOnlyList<BleServiceInfo>> GetServices() => this.Invoke(async module =>
    {
        var uuids = await module.InvokeAsync<string[]>("getServices", this.Uuid).ConfigureAwait(false);
        return (IReadOnlyList<BleServiceInfo>)uuids.Select(x => new BleServiceInfo(x)).ToList();
    });


    public IObservable<BleServiceInfo> GetService(string serviceUuid) => this
        .GetServices()
        .Select(list => list.FirstOrDefault(x => x.Uuid.Equals(serviceUuid, StringComparison.OrdinalIgnoreCase))
            ?? throw new BleException($"Service '{serviceUuid}' not found. Web Bluetooth only exposes services declared when the device was chosen."));


    public IObservable<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristics(string serviceUuid) => this.Invoke(async module =>
    {
        var service = new BleServiceInfo(serviceUuid);
        var results = await module
            .InvokeAsync<JsCharacteristic[]>("getCharacteristics", this.Uuid, serviceUuid)
            .ConfigureAwait(false);

        return (IReadOnlyList<BleCharacteristicInfo>)results
            .Select(x => new BleCharacteristicInfo(service, x.Uuid, false, (CharacteristicProperties)x.Properties))
            .ToList();
    });


    public IObservable<BleCharacteristicInfo> GetCharacteristic(string serviceUuid, string characteristicUuid) => this.Invoke(async module =>
    {
        var result = await module
            .InvokeAsync<JsCharacteristic>("getCharacteristic", this.Uuid, serviceUuid, characteristicUuid)
            .ConfigureAwait(false);

        return new BleCharacteristicInfo(
            new BleServiceInfo(serviceUuid),
            result.Uuid,
            false,
            (CharacteristicProperties)result.Properties
        );
    });


    public IObservable<BleCharacteristicResult> ReadCharacteristic(string serviceUuid, string characteristicUuid) => this.Invoke(async module =>
    {
        var base64 = await module
            .InvokeAsync<string>("readCharacteristic", this.Uuid, serviceUuid, characteristicUuid)
            .ConfigureAwait(false);

        return new BleCharacteristicResult(
            Info(serviceUuid, characteristicUuid),
            BleCharacteristicEvent.Read,
            Convert.FromBase64String(base64)
        );
    });


    public IObservable<BleCharacteristicResult> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true) => this.Invoke(async module =>
    {
        // byte[] crosses to JS as a Uint8Array - no base64 on the write path, which is the hot one.
        await module
            .InvokeVoidAsync("writeCharacteristic", this.Uuid, serviceUuid, characteristicUuid, data, withResponse)
            .ConfigureAwait(false);

        return new BleCharacteristicResult(
            Info(serviceUuid, characteristicUuid),
            withResponse ? BleCharacteristicEvent.Write : BleCharacteristicEvent.WriteWithoutResponse,
            data
        );
    });


    /// <summary>
    /// Subscribes to notifications for the lifetime of the subscription. Web Bluetooth chooses between
    /// notify and indicate from the characteristic's own properties, so
    /// <paramref name="useIndicationsIfAvailable"/> is accepted for interface compatibility and ignored.
    /// </summary>
    public IObservable<BleCharacteristicResult> NotifyCharacteristic(string serviceUuid, string characteristicUuid, bool useIndicationsIfAvailable = true)
        => Observable.Create<BleCharacteristicResult>(ob =>
        {
            var info = Info(serviceUuid, characteristicUuid, isNotifying: true);

            var sub = this.callback
                .WhenNotification()
                .Where(x => x.Service.Equals(serviceUuid, StringComparison.OrdinalIgnoreCase)
                         && x.Characteristic.Equals(characteristicUuid, StringComparison.OrdinalIgnoreCase))
                .Subscribe(x => ob.OnNext(new BleCharacteristicResult(info, BleCharacteristicEvent.Notification, x.Data)));

            _ = Task.Run(async () =>
            {
                try
                {
                    var module = await this.getModule().ConfigureAwait(false);
                    await module
                        .InvokeVoidAsync("startNotifications", this.Uuid, serviceUuid, characteristicUuid, this.callbackRef)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    ob.OnError(new BleException($"Failed to subscribe to '{characteristicUuid}'", ex));
                }
            });

            return new CompositeDisposable(
                sub,
                Disposable.Create(() => _ = Task.Run(async () =>
                {
                    try
                    {
                        var module = await this.getModule().ConfigureAwait(false);
                        await module
                            .InvokeVoidAsync("stopNotifications", this.Uuid, serviceUuid, characteristicUuid)
                            .ConfigureAwait(false);
                    }
                    catch
                    {
                        // best effort - the link may already be gone
                    }
                }))
            );
        });


    public IObservable<BleCharacteristicInfo> WhenCharacteristicSubscriptionChanged(string serviceUuid, string characteristicUuid)
        => Observable.Throw<BleCharacteristicInfo>(
            new NotSupportedException("Web Bluetooth does not report characteristic subscription changes.")
        );


    // ---- descriptors -----------------------------------------------------------------------------

    public IObservable<IReadOnlyList<BleDescriptorInfo>> GetDescriptors(string serviceUuid, string characteristicUuid) => this.Invoke(async module =>
    {
        var info = Info(serviceUuid, characteristicUuid);
        var uuids = await module
            .InvokeAsync<string[]>("getDescriptors", this.Uuid, serviceUuid, characteristicUuid)
            .ConfigureAwait(false);

        return (IReadOnlyList<BleDescriptorInfo>)uuids.Select(x => new BleDescriptorInfo(info, x)).ToList();
    });


    public IObservable<BleDescriptorInfo> GetDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => this
        .GetDescriptors(serviceUuid, characteristicUuid)
        .Select(list => list.FirstOrDefault(x => x.Uuid.Equals(descriptorUuid, StringComparison.OrdinalIgnoreCase))
            ?? throw new BleException($"Descriptor '{descriptorUuid}' not found"));


    public IObservable<BleDescriptorResult> ReadDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => this.Invoke(async module =>
    {
        var base64 = await module
            .InvokeAsync<string>("readDescriptor", this.Uuid, serviceUuid, characteristicUuid, descriptorUuid)
            .ConfigureAwait(false);

        return new BleDescriptorResult(
            new BleDescriptorInfo(Info(serviceUuid, characteristicUuid), descriptorUuid),
            Convert.FromBase64String(base64)
        );
    });


    public IObservable<BleDescriptorResult> WriteDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid, byte[] data) => this.Invoke(async module =>
    {
        await module
            .InvokeVoidAsync("writeDescriptor", this.Uuid, serviceUuid, characteristicUuid, descriptorUuid, data)
            .ConfigureAwait(false);

        return new BleDescriptorResult(
            new BleDescriptorInfo(Info(serviceUuid, characteristicUuid), descriptorUuid),
            data
        );
    });


    // ---- plumbing --------------------------------------------------------------------------------

    IObservable<T> Invoke<T>(Func<IJSObjectReference, Task<T>> operation) => Observable.FromAsync(async () =>
    {
        if (this.status.Value != ConnectionState.Connected)
            throw new BleException("The peripheral is not connected.");

        var module = await this.getModule().ConfigureAwait(false);
        return await operation(module).ConfigureAwait(false);
    });


    static BleCharacteristicInfo Info(string serviceUuid, string characteristicUuid, bool isNotifying = false)
        => new(new BleServiceInfo(serviceUuid), characteristicUuid, isNotifying, (CharacteristicProperties)0);


    class JsCharacteristic
    {
        public string Uuid { get; set; } = null!;
        public int Properties { get; set; }
    }


    public async ValueTask DisposeAsync()
    {
        this.disconnectSub.Dispose();

        try
        {
            var module = await this.getModule().ConfigureAwait(false);
            await module.InvokeVoidAsync("disconnect", this.Uuid).ConfigureAwait(false);
        }
        catch
        {
            // best effort
        }

        this.callbackRef.Dispose();
        this.callback.Dispose();
        this.status.Dispose();
        this.connectionFailed.Dispose();
    }
}

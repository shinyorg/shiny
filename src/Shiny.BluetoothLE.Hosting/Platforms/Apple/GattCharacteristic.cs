using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;

namespace Shiny.BluetoothLE.Hosting;


public class GattCharacteristic : IGattCharacteristic, IGattCharacteristicBuilder, IDisposable
{
    readonly CBPeripheralManager manager = new();
    readonly PeripheralCache cache = new();

    CBMutableCharacteristic? native;
    Func<CharacteristicSubscription, Task>? onSubscribe;
    Func<WriteRequest, Task>? onWrite;
    Func<ReadRequest, Task<GattResult>>? onRead;

    CBAttributePermissions permissions = 0;
    CBCharacteristicProperties properties = 0;


    public GattCharacteristic(CBPeripheralManager manager, string uuid)
    {
        this.manager = manager;
        this.Uuid = uuid;
    }


    public string Uuid { get; }
    public CharacteristicProperties Properties
    {
        get
        {
            var p = (CharacteristicProperties)(int)this.properties;
            return p;
        }
    }
    
    public IReadOnlyList<IPeripheral> SubscribedCentrals => this.cache.Subscribed;


    public Task Notify(byte[] data, params IPeripheral[] centrals)
        => this.Notify(data, CancellationToken.None, centrals);


    public async Task Notify(byte[] data, CancellationToken cancellationToken, params IPeripheral[] centrals)
    {
        if (this.native == null)
            throw new InvalidOperationException("Characteristic has not been built");

        // CoreBluetooth reads a null central list as "every subscriber" - an explicit list goes to just those centrals
        CBCentral[]? targets = null;
        if (centrals.Length > 0)
        {
            targets = centrals.OfType<Peripheral>().Select(x => x.Central).ToArray();
            if (targets.Length == 0)
                return;
        }

        // UpdateValue returns false when the transmit queue is full, and CoreBluetooth then raises
        // ReadyToUpdateSubscribers once. The handlers are attached BEFORE each attempt so that callback cannot
        // fire in the gap between a failed attempt and the subscription, and a retry that finds the queue
        // full again waits for the next callback rather than dropping the value. A power-down never drains
        // the queue, so the wait also ends when the manager leaves PoweredOn or the token is cancelled (#1657)
        var value = NSData.FromArray(data);
        var sent = false;
        while (!sent)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var readyHandler = new EventHandler((_, _) => ready.TrySetResult());
            var stateHandler = new EventHandler((_, _) =>
            {
                if (this.manager.State != CBManagerState.PoweredOn)
                    ready.TrySetException(NotPoweredOn(this.manager.State));
            });

            this.manager.ReadyToUpdateSubscribers += readyHandler;
            this.manager.StateUpdated += stateHandler;
            try
            {
                using var registration = cancellationToken.Register(() => ready.TrySetCanceled(cancellationToken));
                if (this.manager.State != CBManagerState.PoweredOn)
                    throw NotPoweredOn(this.manager.State);

                sent = this.manager.UpdateValue(value, this.native, targets);
                if (!sent)
                    await ready.Task.ConfigureAwait(false);
            }
            finally
            {
                this.manager.ReadyToUpdateSubscribers -= readyHandler;
                this.manager.StateUpdated -= stateHandler;
            }
        }
    }


    static InvalidOperationException NotPoweredOn(CBManagerState state)
        => new($"Bluetooth is not powered on (state: {state}) - the notification was not sent");


    public IGattCharacteristicBuilder SetNotification(Func<CharacteristicSubscription, Task>? onSubscribe = null, NotificationOptions options = NotificationOptions.Notify)
    {
        this.onSubscribe = onSubscribe;
        var enc = options.HasFlag(NotificationOptions.EncryptionRequired);

        if (options.HasFlag(NotificationOptions.Indicate))
        {
            this.properties |= enc
                ? CBCharacteristicProperties.IndicateEncryptionRequired
                : CBCharacteristicProperties.Indicate;
        }

        if (options.HasFlag(NotificationOptions.Notify))
        {
            this.properties |= enc
                ? CBCharacteristicProperties.NotifyEncryptionRequired
                : CBCharacteristicProperties.Notify;
        }
        return this;
    }


    public IGattCharacteristicBuilder SetRead(Func<ReadRequest, Task<GattResult>> onRead, bool encrypted = false)
    {
        this.onRead = onRead;
        this.permissions |= encrypted
            ? CBAttributePermissions.ReadEncryptionRequired
            : CBAttributePermissions.Readable;

        this.properties |= CBCharacteristicProperties.Read;

        return this;
    }


    public IGattCharacteristicBuilder SetWrite(Func<WriteRequest, Task> onWrite, WriteOptions options = WriteOptions.Write)
    {
        this.onWrite = onWrite;
        this.permissions |= options.HasFlag(WriteOptions.EncryptionRequired)
            ? CBAttributePermissions.WriteEncryptionRequired
            : CBAttributePermissions.Writeable;

        if (options.HasFlag(WriteOptions.Write))
            this.properties |= CBCharacteristicProperties.Write;

        if (options.HasFlag(WriteOptions.WriteWithoutResponse))
            this.properties |= CBCharacteristicProperties.WriteWithoutResponse;

        if (options.HasFlag(WriteOptions.AuthenticatedSignedWrites))
            this.properties |= CBCharacteristicProperties.AuthenticatedSignedWrites;

        return this;
    }


    public void Build(CBMutableService service)
    {
        if (this.onWrite != null)
            this.manager.WriteRequestsReceived += this.OnWrite!;

        if (this.onRead != null)
            this.manager.ReadRequestReceived += this.OnRead!;

        // subscriptions are tracked even without a hook - SubscribedCentrals and the generated
        // request/response reply both depend on it
        this.manager.CharacteristicSubscribed += this.OnSubscribed!;
        this.manager.CharacteristicUnsubscribed += this.OnUnSubscribed!;

        this.native = new CBMutableCharacteristic(
            CBUUID.FromString(this.Uuid),
            this.properties,
            null,
            this.permissions
        );
        service.Characteristics = ExpandArray(service.Characteristics!, this.native);
    }


    static T[] ExpandArray<T>(T[] array, params T[] items)
    {
        array = array ?? new T[0];
        var newArray = new T[array.Length + items.Length];
        Array.Copy(array, newArray, array.Length);
        Array.Copy(items, 0, newArray, array.Length, items.Length);
        return newArray;
    }


    public void Dispose()
    {
        this.manager.WriteRequestsReceived -= this.OnWrite!;
        this.manager.ReadRequestReceived -= this.OnRead!;
        this.manager.CharacteristicSubscribed -= this.OnSubscribed!;
        this.manager.CharacteristicUnsubscribed -= this.OnUnSubscribed!;
    }


    bool IsThis(CBCharacteristic arg) => this.native?.Equals(arg) ?? false;

    async void OnSubChanged(CBPeripheralManagerSubscriptionEventArgs args, bool subscribed)
    {
        if (!this.IsThis(args.Characteristic))
            return;

        var peripheral = this.cache.SetSubscription(args.Central, subscribed);
        if (this.onSubscribe != null)
            await this.onSubscribe.Invoke(new CharacteristicSubscription(this, peripheral, subscribed));
    }


    void OnSubscribed(object sender, CBPeripheralManagerSubscriptionEventArgs args) => this.OnSubChanged(args, true);
    void OnUnSubscribed(object sender, CBPeripheralManagerSubscriptionEventArgs args) => this.OnSubChanged(args, false);


    async void OnWrite(object sender, CBATTRequestsEventArgs args)
    {
        foreach (var req in args.Requests)
        {
            if (this.IsThis(req.Characteristic))
            {
                var responded = false;
                var peripheral = this.cache.GetOrAdd(req.Central);
                await this.onWrite!.Invoke(new WriteRequest(
                    this,
                    peripheral,
                    req.Value?.ToArray(),
                    (int)req.Offset,
                    true,
                    (status) =>
                    {
                        responded = true;
                        var nativeStatus = Enum.Parse<CBATTError>(status.ToString(), true);
                        this.manager.RespondToRequest(req, nativeStatus);
                    }
                ));
                if (!responded)
                    this.manager.RespondToRequest(req, CBATTError.Success);
            }
        }
    }


    async void OnRead(object sender, CBATTRequestEventArgs args)
    {
        if (!args.Request.Characteristic.Equals(this.native))
            return;

        var peripheral = this.cache.GetOrAdd(args.Request.Central);
        var result = await this.onRead!.Invoke(new ReadRequest(
            this,
            peripheral,
            (int)args.Request.Offset)
        );
        if (result.Status == GattState.Success)
        {
            args.Request.Value = NSData.FromArray(result.Data!);
            this.manager.RespondToRequest(args.Request, CBATTError.Success);
        }
        else
        {
            this.manager.RespondToRequest(args.Request, CBATTError.InsufficientEncryption);
        }
    }
}

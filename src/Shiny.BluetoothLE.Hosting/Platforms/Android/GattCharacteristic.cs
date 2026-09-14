using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Bluetooth;
using Java.Lang.Annotation;
using Shiny.BluetoothLE.Hosting.Internals;

namespace Shiny.BluetoothLE.Hosting;


public class GattCharacteristic : IGattCharacteristic, IGattCharacteristicBuilder, IDisposable
{
    readonly GattServerContext context;
    readonly CompositeDisposable disposer = new();
    readonly Dictionary<string, IPeripheral> subscribers = new();
    readonly Dictionary<string, bool> indicating = new();
    Func<CharacteristicSubscription, Task>? onSubscribe;
    Func<WriteRequest, Task>? onWrite;
    Func<ReadRequest, Task<GattResult>>? onRead;
    GattProperty properties = 0;
    GattPermission permissions = 0;


    public GattCharacteristic(GattServerContext context, string uuid)
    {
        this.context = context;
        this.Uuid = uuid;
    }


    public BluetoothGattCharacteristic Native { get; private set; } = null!;
    public string Uuid { get; }
    public CharacteristicProperties Properties => (CharacteristicProperties)(int)this.properties;
    public IReadOnlyList<IPeripheral> SubscribedCentrals
    {
        get
        {
            lock (this.subscribers)
            {
                return this.subscribers.Values.ToList();
            }
        }
    }


    public Task Notify(byte[] data, params IPeripheral[] centrals)
        => this.Notify(data, CancellationToken.None, centrals);


    public async Task Notify(byte[] data, CancellationToken cancellationToken, params IPeripheral[] centrals)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // an empty list means every subscriber - OfType never returns null, so a `??` fallback never ran
        // and a broadcast went to nobody
        var sendTo = centrals.Length == 0
            ? this.SubscribedCentrals.OfType<Peripheral>().ToArray()
            : centrals.OfType<Peripheral>().ToArray();

        await Task
            .WhenAll(sendTo.Select(x => this.NotifyDevice(x.Native, data, cancellationToken)))
            .ConfigureAwait(false);
    }


    async Task NotifyDevice(BluetoothDevice device, byte[] data, CancellationToken cancellationToken)
    {
        // Android keeps one notification in flight per remote device and refuses the next until
        // OnNotificationSent arrives - sending without waiting returned busy (or false) and the value was
        // silently dropped (#1657). The gate is per device across the whole server, not per characteristic
        var gate = this.context.GetNotificationGate(device);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // null means the central went away before the stack reported the send - nothing to deliver to
            var sent = new TaskCompletionSource<GattStatus?>(TaskCreationOptions.RunContinuationsAsynchronously);

            // everything that can end the wait is hooked BEFORE the notification goes out, so a callback
            // that lands immediately cannot be missed
            using var sentSub = this.context
                .NotificationSent
                .Where(x => x.Device.Address == device.Address)
                .Take(1)
                .Subscribe(x => sent.TrySetResult(x.Status));

            using var disconnectSub = this.context
                .ConnectionStateChanged
                .Where(x => x.Device.Address == device.Address && x.NewState == ProfileState.Disconnected)
                .Subscribe(_ => sent.TrySetResult(null));

            using var closedSub = this.context.ServerClosed.Subscribe(_ => sent.TrySetResult(null));
            using var registration = cancellationToken.Register(() => sent.TrySetCanceled(cancellationToken));

            this.SendNotification(device, data);

            var status = await sent.Task.ConfigureAwait(false);
            if (status != null && status != GattStatus.Success)
                throw new InvalidOperationException($"Notification to '{device.Address}' failed with status {status}");
        }
        finally
        {
            gate.Release();
        }
    }


    void SendNotification(BluetoothDevice device, byte[] data)
    {
        // confirm = true makes it an indication, which is what the central asked for when it wrote the CCCD
        bool confirm;
        lock (this.subscribers)
            confirm = this.indicating.TryGetValue(device.Address!, out var indicate) && indicate;

        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var status = this.context.Server.NotifyCharacteristicChanged(device, this.Native, confirm, data);
            if (status != (int)CurrentBluetoothStatusCodes.Success)
                throw new InvalidOperationException($"Notification to '{device.Address}' was refused - Android status code {status}");
        }
        else
        {
            // before API 33 the value travels on the shared characteristic, so setting it and sending must not
            // interleave with a send to another central
            lock (this.Native)
            {
                this.Native.SetValue(data);
                if (!this.context.Server.NotifyCharacteristicChanged(device, this.Native, confirm))
                    throw new InvalidOperationException($"Notification to '{device.Address}' was refused");
            }
        }
    }


    public IGattCharacteristicBuilder SetNotification(Func<CharacteristicSubscription, Task>? onSubscribe = null, NotificationOptions options = NotificationOptions.Notify)
    {
        this.onSubscribe = onSubscribe;
        if (options.HasFlag(NotificationOptions.Indicate))
            this.properties |= GattProperty.Indicate;

        if (options.HasFlag(NotificationOptions.Notify))
            this.properties |= GattProperty.Notify;

        return this;
    }


    public IGattCharacteristicBuilder SetWrite(Func<WriteRequest, Task> onWrite, WriteOptions options = WriteOptions.Write)
    {
        this.onWrite = onWrite;
        if (options.HasFlag(WriteOptions.EncryptionRequired))
        {
            this.permissions = GattPermission.WriteEncrypted;
        }
        else if (options.HasFlag(WriteOptions.AuthenticatedSignedWrites))
        {
            this.properties |= GattProperty.SignedWrite;
            this.permissions |= GattPermission.WriteSigned;
        }
        else
        {
            this.properties |= GattProperty.Write;
            this.permissions |= GattPermission.Write;
        }
        if (options.HasFlag(WriteOptions.WriteWithoutResponse))
            this.properties |= GattProperty.WriteNoResponse;

        return this;
    }


    public IGattCharacteristicBuilder SetRead(Func<ReadRequest, Task<GattResult>> onRead, bool encrypted = false)
    {
        this.onRead = onRead;
        this.properties |= GattProperty.Read;
        if (encrypted)
            this.permissions |= GattPermission.ReadEncrypted;
        else
            this.permissions |= GattPermission.Read;

        return this;
    }


    public void Build()
    {
        var nativeUuid = Utils.ToUuidType(this.Uuid);

        this.Native = new BluetoothGattCharacteristic(
            nativeUuid,
            this.properties,
            this.permissions
        );

        this.SetupNotifications();
        this.SetupRead();
        this.SetupWrite();
    }


    public void Dispose() => this.disposer.Dispose();


    void SetupNotifications()
    {
        // the CCCD and subscription tracking are needed whenever the characteristic can notify, hook or not -
        // without the descriptor a central cannot subscribe at all
        if (!this.properties.HasFlag(GattProperty.Notify) && !this.properties.HasFlag(GattProperty.Indicate))
            return;

        var ndesc = new BluetoothGattDescriptor(
            Constants.NotifyDescriptorUuid,
            GattDescriptorPermission.Read | GattDescriptorPermission.Write
        );
        this.Native.AddDescriptor(ndesc);

        this.context
            .DescriptorWrite
            .Where(x => x.Descriptor.Equals(ndesc))
            .Subscribe(async x =>
            {
                var respond = true;
                if (x.Value.SequenceEqual(Constants.IndicateEnableBytes) || x.Value.SequenceEqual(Constants.NotifyEnableBytes))
                {
                    var peripheral = this.GetOrAdd(x.Device);
                    lock (this.subscribers)
                        this.indicating[x.Device.Address!] = x.Value.SequenceEqual(Constants.IndicateEnableBytes);

                    if (this.onSubscribe != null)
                        await this.onSubscribe(new CharacteristicSubscription(this, peripheral, true)).ConfigureAwait(false);
                }
                else if (x.Value.SequenceEqual(Constants.NotifyDisableBytes))
                {
                    var peripheral = this.Remove(x.Device);
                    if (peripheral != null && this.onSubscribe != null)
                        await this.onSubscribe(new CharacteristicSubscription(this, peripheral, false)).ConfigureAwait(false);
                }
                else
                { 
                    respond = false;
                }
                if (respond && x.ResponseNeeded)
                {
                    this.context.Server.SendResponse(
                        x.Device,
                        x.RequestId,
                        GattStatus.Success,
                        0,
                        new byte[] { 0x1 }
                    );
                }
            })
            .DisposedBy(this.disposer);

        this.context
            .ConnectionStateChanged
            .Where(x => x.NewState == ProfileState.Disconnected)
            .Subscribe(async x =>
            {
                var peripheral = this.Remove(x.Device);
                if (peripheral != null && this.onSubscribe != null)
                    await this.onSubscribe(new CharacteristicSubscription(this, peripheral, false)).ConfigureAwait(false);
            })
            .DisposedBy(this.disposer);
    }


    void SetupRead()
    {
        if (this.onRead == null)
            return;

        this.context
            .CharacteristicRead
            .Where(x => x.Characteristic.Equals(this.Native))
            .Subscribe(async ch =>
            {
                var peripheral = new Peripheral(ch.Device);
                var request = new ReadRequest(this, peripheral, ch.Offset);
                var result = await this.onRead(request).ConfigureAwait(false);

                this.context.Server.SendResponse
                (
                    ch.Device,
                    ch.RequestId,
                    result.Status.ToNative(),
                    ch.Offset,
                    result.Data!
                );
            })
            .DisposedBy(this.disposer);
    }


    void SetupWrite()
    {
        if (this.onWrite == null)
            return;

        this.context
            .CharacteristicWrite
            .Where(x => x.Characteristic.Equals(this.Native))
            .Subscribe(async ch =>
            {
                var responded = false;
                var peripheral = new Peripheral(ch.Device);
                var request = new WriteRequest(
                    this,
                    peripheral,
                    ch.Value,
                    ch.Offset,
                    ch.ResponseNeeded,
                    (status) =>
                    {
                        responded = true;
                        this.context.Server.SendResponse(
                            ch.Device,
                            ch.RequestId,
                            status.ToNative(),
                            ch.Offset,
                            ch.Value
                        );
                    }
                );
                await this.onWrite(request).ConfigureAwait(false);
                if (request.IsReplyNeeded && !responded)
                {
                    this.context.Server.SendResponse(
                        ch.Device,
                        ch.RequestId,
                        GattStatus.Success,
                        ch.Offset,
                        ch.Value
                    );
                }
            })
            .DisposedBy(this.disposer);
    }


    void SetupMtuChanged()
    {
        this.context
            .MtuChanged
            .Where(x => this.subscribers.ContainsKey(x.Device.Address!))
            .Subscribe(ch =>
            {
                var peripheral = this.subscribers[ch.Device.Address!] as Peripheral;
                if (peripheral != null)
                    peripheral.Mtu = ch.Mtu - BleConstants.AttHeaderSize;
            })
            .DisposedBy(this.disposer);
    }


    IPeripheral GetOrAdd(BluetoothDevice native)
    {
        lock (this.subscribers)
        {
            if (this.subscribers.ContainsKey(native.Address!))
                return this.subscribers[native.Address!];

            var device = new Peripheral(native);
            this.subscribers.Add(native.Address!, device);
            return device;
        }
    }


    IPeripheral? Remove(BluetoothDevice native)
    {
        lock (this.subscribers)
        {
            if (this.subscribers.ContainsKey(native.Address!))
            {
                var device = this.subscribers[native.Address!];
                this.subscribers.Remove(native.Address!);
                return device;
            }
            return null;
        }
    }
}


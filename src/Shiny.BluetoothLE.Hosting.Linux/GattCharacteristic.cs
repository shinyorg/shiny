using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Hosting;


public class GattCharacteristic : IGattCharacteristic, IGattCharacteristicBuilder
{
    readonly ConcurrentDictionary<string, Peripheral> subscribers = new();

    internal Func<ReadRequest, Task<GattResult>>? OnReadHandler { get; private set; }
    internal Func<WriteRequest, Task>? OnWriteHandler { get; private set; }
    internal Func<CharacteristicSubscription, Task>? OnSubscribeHandler { get; private set; }

    internal CharacteristicProperties NativeProperties { get; private set; }
    internal bool ReadEncrypted { get; private set; }
    internal WriteOptions WriteOptions { get; private set; }
    internal NotificationOptions NotifyOptions { get; private set; }

    /// <summary>
    /// BlueZ object path for this characteristic. Set when the parent service is registered.
    /// </summary>
    public string? ObjectPath { get; internal set; }

    /// <summary>
    /// BlueZ object path of the service this characteristic belongs to.
    /// </summary>
    internal string? ServicePath { get; set; }

    /// <summary>
    /// Hands a value to BlueZ as a <c>PropertiesChanged</c> signal. Set while the parent service is registered.
    /// </summary>
    internal Action<byte[]>? NotifyDispatcher { get; set; }

    /// <summary>
    /// Whether BlueZ reports at least one central with notifications or indications enabled.
    /// </summary>
    internal bool IsNotifying { get; private set; }


    public GattCharacteristic(string uuid) => this.Uuid = uuid;


    public string Uuid { get; }
    public CharacteristicProperties Properties => this.NativeProperties;
    public IReadOnlyList<IPeripheral> SubscribedCentrals => this.subscribers.Values.Cast<IPeripheral>().ToList();


    public IGattCharacteristicBuilder SetRead(Func<ReadRequest, Task<GattResult>> request, bool encrypted = false)
    {
        this.OnReadHandler = request;
        this.ReadEncrypted = encrypted;
        this.NativeProperties |= CharacteristicProperties.Read;
        return this;
    }


    public IGattCharacteristicBuilder SetWrite(Func<WriteRequest, Task> request, WriteOptions options = WriteOptions.Write)
    {
        this.OnWriteHandler = request;
        this.WriteOptions = options;

        if (options.HasFlag(WriteOptions.Write))
            this.NativeProperties |= CharacteristicProperties.Write;

        if (options.HasFlag(WriteOptions.WriteWithoutResponse))
            this.NativeProperties |= CharacteristicProperties.WriteWithoutResponse;

        if (options.HasFlag(WriteOptions.AuthenticatedSignedWrites))
            this.NativeProperties |= CharacteristicProperties.AuthenticatedSignedWrites;

        return this;
    }


    public IGattCharacteristicBuilder SetNotification(Func<CharacteristicSubscription, Task>? onSubscribe = null, NotificationOptions options = NotificationOptions.Notify)
    {
        this.OnSubscribeHandler = onSubscribe;
        this.NotifyOptions = options;

        if (options.HasFlag(NotificationOptions.Indicate))
            this.NativeProperties |= CharacteristicProperties.Indicate;
        else
            this.NativeProperties |= CharacteristicProperties.Notify;

        return this;
    }


    public Task Notify(byte[] data, params IPeripheral[] centrals)
        => this.Notify(data, CancellationToken.None, centrals);


    public Task Notify(byte[] data, CancellationToken cancellationToken, params IPeripheral[] centrals)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (this.NotifyDispatcher == null)
            throw new InvalidOperationException("Characteristic has not been registered with BlueZ yet");

        // nobody has notifications enabled - there is nothing to send
        if (!this.IsNotifying)
            return Task.CompletedTask;

        // BlueZ fans a Value change out to every subscribed central and offers an external application no way
        // to address one, so the centrals named here cannot narrow the recipients. The send is skipped only when
        // none of them is subscribed
        if (centrals.Length > 0 && !centrals.Any(this.IsSubscribed))
            return Task.CompletedTask;

        this.NotifyDispatcher(data);
        return Task.CompletedTask;
    }


    /// <summary>
    /// BlueZ called StartNotify or StopNotify. It reports only that notifications are on for somebody, never
    /// which central enabled them, so while they are on every connected central is treated as subscribed.
    /// </summary>
    internal void SetNotifying(bool notifying, IReadOnlyList<Peripheral> connected)
    {
        this.IsNotifying = notifying;
        if (notifying)
        {
            foreach (var peripheral in connected)
                this.AddSubscriber(peripheral);
        }
        else
        {
            foreach (var devicePath in this.subscribers.Keys.ToList())
                this.RemoveSubscriber(devicePath);
        }
    }


    internal void OnDeviceConnectionChanged(Peripheral peripheral, bool connected)
    {
        if (!connected)
            this.RemoveSubscriber(peripheral.DevicePath);
        else if (this.IsNotifying)
            this.AddSubscriber(peripheral);
    }


    bool IsSubscribed(IPeripheral central) => central is Peripheral linux
        ? this.subscribers.ContainsKey(linux.DevicePath)
        : this.subscribers.Values.Any(x => x.Uuid.Equals(central.Uuid, StringComparison.OrdinalIgnoreCase));


    internal void AddSubscriber(Peripheral peripheral)
    {
        if (this.subscribers.TryAdd(peripheral.DevicePath, peripheral) && this.OnSubscribeHandler != null)
            _ = this.OnSubscribeHandler(new CharacteristicSubscription(this, peripheral, true));
    }


    internal void RemoveSubscriber(string devicePath)
    {
        if (this.subscribers.TryRemove(devicePath, out var peripheral) && this.OnSubscribeHandler != null)
            _ = this.OnSubscribeHandler(new CharacteristicSubscription(this, peripheral, false));
    }
}

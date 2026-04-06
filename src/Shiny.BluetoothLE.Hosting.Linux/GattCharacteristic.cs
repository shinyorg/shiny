using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

    internal Func<byte[], IReadOnlyCollection<Peripheral>, Task>? NotifyDispatcher { get; set; }


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
    {
        if (this.NotifyDispatcher == null)
            throw new InvalidOperationException("Characteristic has not been registered with BlueZ yet");

        IReadOnlyCollection<Peripheral> targets = centrals.Length == 0
            ? this.subscribers.Values.ToList()
            : centrals.OfType<Peripheral>().ToList();

        return this.NotifyDispatcher(data, targets);
    }


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

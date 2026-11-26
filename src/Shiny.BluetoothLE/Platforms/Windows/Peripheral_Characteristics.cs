using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Shiny.BluetoothLE.Infrastructure;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    public IObservable<BleCharacteristicInfo> GetCharacteristic(string serviceUuid, string characteristicUuid) => ObservableTask.FromAsync<BleCharacteristicInfo>(async ct =>
    {
        this.AssertConnection();

        var ch = await this.GetNativeCharacteristicAsync(serviceUuid, characteristicUuid, ct).ConfigureAwait(false);
        return ToCharInfo(ch);
    });


    public IObservable<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristics(string serviceUuid) => ObservableTask.FromAsync(async ct =>
    {
        this.AssertConnection();

        var service = await this.GetNativeServiceAsync(serviceUuid, ct).ConfigureAwait(false);
        var chResult = await service
            .GetCharacteristicsAsync(BluetoothCacheMode.Uncached)
            .AsTask(ct)
            .ConfigureAwait(false);

        chResult.Status.Assert("GetCharacteristics", serviceUuid);
        return (IReadOnlyList<BleCharacteristicInfo>)chResult.Characteristics.Select(x => ToCharInfo(x)).ToList();
    });


    readonly Subject<BleCharacteristicInfo> charSubChangedSubj = new();
    public IObservable<BleCharacteristicInfo> WhenCharacteristicSubscriptionChanged(string serviceUuid, string characteristicUuid)
    {
        var key = $"{serviceUuid}-{characteristicUuid}";
        return this.charSubChangedSubj
            .Where(x =>
                x.Service.Uuid.Equals(serviceUuid, StringComparison.OrdinalIgnoreCase) &&
                x.Uuid.Equals(characteristicUuid, StringComparison.OrdinalIgnoreCase)
            );
    }


    // One stream per characteristic, shared by RefCount: the first subscriber writes the CCCD, the last one leaving
    // writes None and unhooks ValueChanged. It is not torn down on disconnect - it re-hooks itself on WhenConnected - and
    // drops out of the cache when it ends, so an errored stream isn't replayed to later callers.
    readonly Dictionary<string, IObservable<BleCharacteristicResult>> notifiers = new();
    public IObservable<BleCharacteristicResult> NotifyCharacteristic(string serviceUuid, string characteristicUuid, bool useIndicationsIfAvailable = true)
    {
        this.AssertConnection();

        var key = $"{serviceUuid}-{characteristicUuid}";

        lock (this.notifiers)
        {
            if (this.notifiers.TryGetValue(key, out var existing))
                return existing;

            IObservable<BleCharacteristicResult>? obs = null;
            obs = this.WhenConnected()
                .Select(_ => ObservableTask.FromAsync(ct => this.GetNativeCharacteristicAsync(serviceUuid, characteristicUuid, ct)))
                .Switch()
                .Select(ch => Observable.Create<BleCharacteristicResult>(ob =>
                {
                    var characteristicInfo = ToCharInfo(ch);
                    var notifyingInfo = ToCharInfo(ch, isNotifying: true);
                    var notifyingOffInfo = ToCharInfo(ch, isNotifying: false);

                    var handler = new TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs>((sender, args) =>
                    {
                        var data = args.CharacteristicValue?.ToArray();
                        ob.OnNext(new BleCharacteristicResult(characteristicInfo, BleCharacteristicEvent.Notification, data));
                    });

                    ch.ValueChanged += handler;

                    var notifyValue = useIndicationsIfAvailable && ch.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate)
                        ? GattClientCharacteristicConfigurationDescriptorValue.Indicate
                        : GattClientCharacteristicConfigurationDescriptorValue.Notify;

                    ch.WriteClientCharacteristicConfigurationDescriptorAsync(notifyValue)
                        .AsTask()
                        .ContinueWith(task =>
                        {
                            if (task.IsFaulted)
                            {
                                ob.OnError(task.Exception!.InnerException ?? task.Exception);
                            }
                            else if (task.Result != GattCommunicationStatus.Success)
                            {
                                ob.OnError(new BleException($"Failed to enable notifications: {task.Result}"));
                            }
                            else
                            {
                                this.charSubChangedSubj.OnNext(notifyingInfo);
                            }
                        });

                    return () =>
                    {
                        ch.ValueChanged -= handler;

                        try
                        {
                            ch.WriteClientCharacteristicConfigurationDescriptorAsync(
                                GattClientCharacteristicConfigurationDescriptorValue.None
                            ).AsTask().ContinueWith(task =>
                            {
                                if (!task.IsFaulted && task.Result == GattCommunicationStatus.Success)
                                    this.charSubChangedSubj.OnNext(notifyingOffInfo);
                            });
                        }
                        catch
                        {
                            // Device may already be disconnected
                        }
                    };
                }))
                .Switch()
                .Finally(() => this.RemoveNotifier(key, obs))
                .Publish()
                .RefCount();

            this.notifiers.Add(key, obs);
            return obs;
        }
    }


    void RemoveNotifier(string key, IObservable<BleCharacteristicResult>? stream)
    {
        lock (this.notifiers)
        {
            var isCurrent = this.notifiers.TryGetValue(key, out var current) && ReferenceEquals(current, stream);
            if (isCurrent)
                this.notifiers.Remove(key);
        }
    }


    public IObservable<BleCharacteristicResult> ReadCharacteristic(string serviceUuid, string characteristicUuid) => ObservableTask.FromAsync(async ct =>
    {
        this.AssertConnection();

        var ch = await this.GetNativeCharacteristicAsync(serviceUuid, characteristicUuid, ct).ConfigureAwait(false);

        var result = await ch
            .ReadValueAsync(BluetoothCacheMode.Uncached)
            .AsTask(ct)
            .ConfigureAwait(false);

        result.Status.Assert("Read", serviceUuid, characteristicUuid);

        return new BleCharacteristicResult(
            ToCharInfo(ch),
            BleCharacteristicEvent.Read,
            result.Value?.ToArray()
        );
    });


    public IObservable<BleCharacteristicResult> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true) => ObservableTask.FromAsync(async ct =>
    {
        this.AssertConnection();

        var ch = await this.GetNativeCharacteristicAsync(serviceUuid, characteristicUuid, ct).ConfigureAwait(false);

        var writeType = withResponse
            ? GattWriteOption.WriteWithResponse
            : GattWriteOption.WriteWithoutResponse;

        var buffer = data.AsBuffer();
        var result = await ch
            .WriteValueAsync(buffer, writeType)
            .AsTask(ct)
            .ConfigureAwait(false);

        result.Assert("Write", serviceUuid, characteristicUuid);

        var ev = withResponse ? BleCharacteristicEvent.Write : BleCharacteristicEvent.WriteWithoutResponse;
        return new BleCharacteristicResult(
            ToCharInfo(ch),
            ev,
            data
        );
    });


    protected static BleCharacteristicInfo ToCharInfo(GattCharacteristic ch, bool? isNotifying = null) => new(
        new BleServiceInfo(ch.Service.Uuid.ToString()),
        ch.Uuid.ToString(),
        isNotifying ?? false,
        MapProperties(ch.CharacteristicProperties)
    );


    static CharacteristicProperties MapProperties(GattCharacteristicProperties native)
    {
        var props = (CharacteristicProperties)0;

        if (native.HasFlag(GattCharacteristicProperties.Broadcast))
            props |= CharacteristicProperties.Broadcast;

        if (native.HasFlag(GattCharacteristicProperties.Read))
            props |= CharacteristicProperties.Read;

        if (native.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
            props |= CharacteristicProperties.WriteWithoutResponse;

        if (native.HasFlag(GattCharacteristicProperties.Write))
            props |= CharacteristicProperties.Write;

        if (native.HasFlag(GattCharacteristicProperties.Notify))
            props |= CharacteristicProperties.Notify;

        if (native.HasFlag(GattCharacteristicProperties.Indicate))
            props |= CharacteristicProperties.Indicate;

        if (native.HasFlag(GattCharacteristicProperties.AuthenticatedSignedWrites))
            props |= CharacteristicProperties.AuthenticatedSignedWrites;

        if (native.HasFlag(GattCharacteristicProperties.ExtendedProperties))
            props |= CharacteristicProperties.ExtendedProperties;

        return props;
    }


    // Cached per connection (cleared on release and on GattServicesChanged). Every operation used to run an uncached
    // service and characteristic discovery and keep the new GattDeviceService until disconnect - a chunked write of
    // thousands of packets opened thousands of service handles and twice as many radio round trips.
    readonly Dictionary<Guid, GattDeviceService> serviceCache = new();
    readonly Dictionary<(Guid Service, Guid Characteristic), GattCharacteristic> characteristicCache = new();


    protected async System.Threading.Tasks.Task<GattDeviceService> GetNativeServiceAsync(string serviceUuid, System.Threading.CancellationToken ct)
    {
        var suid = Utils.ToUuidType(serviceUuid);
        lock (this.trackedServices)
        {
            if (this.serviceCache.TryGetValue(suid, out var cached))
                return cached;
        }

        var result = await this.Native!
            .GetGattServicesForUuidAsync(suid, BluetoothCacheMode.Uncached)
            .AsTask(ct)
            .ConfigureAwait(false);

        result.Status.Assert("GetService", serviceUuid);

        var service = result.Services.FirstOrDefault()
            ?? throw new BleException($"Service '{serviceUuid}' not found");

        foreach (var extra in result.Services.Skip(1))
            extra.Dispose();

        lock (this.trackedServices)
        {
            // a concurrent lookup may have won - keep its object, the tracked set disposes this one with the connection
            this.trackedServices.Add(service);
            if (this.serviceCache.TryGetValue(suid, out var raced))
                return raced;

            this.serviceCache[suid] = service;
        }
        return service;
    }


    protected async System.Threading.Tasks.Task<GattCharacteristic> GetNativeCharacteristicAsync(
        string serviceUuid,
        string characteristicUuid,
        System.Threading.CancellationToken ct)
    {
        var key = (Utils.ToUuidType(serviceUuid), Utils.ToUuidType(characteristicUuid));
        lock (this.trackedServices)
        {
            if (this.characteristicCache.TryGetValue(key, out var cached))
                return cached;
        }

        var service = await this.GetNativeServiceAsync(serviceUuid, ct).ConfigureAwait(false);
        var chResult = await service
            .GetCharacteristicsForUuidAsync(key.Item2, BluetoothCacheMode.Uncached)
            .AsTask(ct)
            .ConfigureAwait(false);

        chResult.Status.Assert("GetCharacteristic", serviceUuid, characteristicUuid);

        var ch = chResult.Characteristics.FirstOrDefault()
            ?? throw new BleException($"Characteristic '{characteristicUuid}' not found in service '{serviceUuid}'");

        lock (this.trackedServices)
            this.characteristicCache[key] = ch;

        return ch;
    }
}

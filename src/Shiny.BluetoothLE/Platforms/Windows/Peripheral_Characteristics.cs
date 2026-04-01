using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    public IObservable<BleCharacteristicInfo> GetCharacteristic(string serviceUuid, string characteristicUuid) => Observable.FromAsync<BleCharacteristicInfo>(async ct =>
    {
        this.AssertConnection();

        var ch = await this.GetNativeCharacteristicAsync(serviceUuid, characteristicUuid, ct).ConfigureAwait(false);
        return ToCharInfo(ch);
    });


    public IObservable<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristics(string serviceUuid) => Observable.FromAsync(async ct =>
    {
        this.AssertConnection();

        var suid = Utils.ToUuidType(serviceUuid);
        var result = await this.Native!.GetGattServicesForUuidAsync(suid, BluetoothCacheMode.Cached).AsTask(ct).ConfigureAwait(false);
        result.Status.Assert("GetServices", serviceUuid);

        var service = result.Services.FirstOrDefault()
            ?? throw new BleException($"Service '{serviceUuid}' not found");
        this.TrackService(service);

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


    readonly Dictionary<string, IObservable<BleCharacteristicResult>> notifiers = new();
    public IObservable<BleCharacteristicResult> NotifyCharacteristic(string serviceUuid, string characteristicUuid, bool useIndicationsIfAvailable = true)
    {
        this.AssertConnection();

        var key = $"{serviceUuid}-{characteristicUuid}";

        if (!this.notifiers.ContainsKey(key))
        {
            var obs = this.WhenConnected()
                .Select(_ => Observable.FromAsync(ct => this.GetNativeCharacteristicAsync(serviceUuid, characteristicUuid, ct)))
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
                            ).AsTask().ContinueWith(_ =>
                            {
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
                .Publish()
                .RefCount();

            this.notifiers.Add(key, obs);
        }
        return this.notifiers[key];
    }


    public IObservable<BleCharacteristicResult> ReadCharacteristic(string serviceUuid, string characteristicUuid) => Observable.FromAsync(async ct =>
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


    public IObservable<BleCharacteristicResult> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true) => Observable.FromAsync(async ct =>
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


    protected void ClearNotifications() => this.notifiers.Clear();


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


    protected async System.Threading.Tasks.Task<GattCharacteristic> GetNativeCharacteristicAsync(
        string serviceUuid,
        string characteristicUuid,
        System.Threading.CancellationToken ct)
    {
        var suid = Utils.ToUuidType(serviceUuid);

        var result = await this.Native!
            .GetGattServicesForUuidAsync(suid, BluetoothCacheMode.Cached)
            .AsTask(ct)
            .ConfigureAwait(false);

        result.Status.Assert("GetService", serviceUuid);

        var service = result.Services.FirstOrDefault()
            ?? throw new BleException($"Service '{serviceUuid}' not found");
        this.TrackService(service);

        var cuid = Utils.ToUuidType(characteristicUuid);
        var chResult = await service
            .GetCharacteristicsForUuidAsync(cuid, BluetoothCacheMode.Cached)
            .AsTask(ct)
            .ConfigureAwait(false);

        chResult.Status.Assert("GetCharacteristic", serviceUuid, characteristicUuid);

        var ch = chResult.Characteristics.FirstOrDefault()
            ?? throw new BleException($"Characteristic '{characteristicUuid}' not found in service '{serviceUuid}'");

        return ch;
    }
}

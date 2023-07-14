using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    public IObservable<BleCharacteristicInfo> GetCharacteristic(string serviceUuid, string characteristicUuid) => Observable.FromAsync<BleCharacteristicInfo>(async ct =>
    {
        var sid = Utils.ToUuidType(serviceUuid);
        var cid = Utils.ToUuidType(characteristicUuid);

        var service = await this.Native!
            .GetGattServicesForUuidAsync(sid)
            .AsTask(ct)
            .ConfigureAwait(false);

        //service.Status.Assert();

        //service.ProtocolError
        var chars = await service.Services.First().GetCharacteristicsForUuidAsync(cid).AsTask(ct).ConfigureAwait(false);
        //chars.Status.Assert();
        var ch = chars.Characteristics.First();

        return ToChar(ch);
    });


    public IObservable<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristics(string serviceUuid) => Observable.FromAsync(async ct =>
    {
        var suid = Utils.ToUuidType(serviceUuid);

        var result = await this.Native!.GetGattServicesForUuidAsync(suid, BluetoothCacheMode.Cached).AsTask();
        result.Status.Assert("Get", serviceUuid);

        var service = result.Services.FirstOrDefault();

        var chResult = await service
            .GetCharacteristicsAsync(BluetoothCacheMode.Uncached)
            .AsTask(ct)
            .ConfigureAwait(false);

        chResult.Status.Assert("GetCharacteristics", serviceUuid);
        return chResult.Characteristics.Select(ToChar).ToList();
    });


    public IObservable<BleCharacteristicInfo> WhenCharacteristicSubscriptionChanged(string serviceUuid, string characteristicUuid) => throw new NotImplementedException();


    readonly Dictionary<string, IObservable<BleCharacteristicResult>> notifiers = new();
    public IObservable<BleCharacteristicResult> NotifyCharacteristic(string serviceUuid, string characteristicUuid, bool useIndicationsIfAvailable = true)
    {
        var key = $"{serviceUuid}-{characteristicUuid}";

        if (this.notifiers.ContainsKey(key))
        {
            var obs = this.WhenConnected()
                .Select(_ => this.GetNativeCharacteristic(serviceUuid, characteristicUuid))
                .Switch()
                .Select(x => Observable.Create<BleCharacteristicResult>(ob =>
                {
                    var handler = new TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs>((sender, args) =>
                    {
                        //args.CharacteristicValue.AsStream()
                    });
                    x.ValueChanged += handler;
                    var notifyValue = useIndicationsIfAvailable && x.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate)
                        ? GattClientCharacteristicConfigurationDescriptorValue.Indicate
                        : GattClientCharacteristicConfigurationDescriptorValue.Notify;

                    x.WriteClientCharacteristicConfigurationDescriptorAsync(notifyValue)
                        .AsTask()
                        .ContinueWith(result =>
                        {

                        });
                    // TODO: fire that it is hooked

                    return () => x.ValueChanged -= handler;
                }))
                .Switch()
                .Finally(() =>
                {
                    // TODO: unhook
                })
                .Publish()
                .RefCount();

            this.notifiers.Add(key, obs);
        }
        return this.notifiers[key];
    }


    public IObservable<BleCharacteristicResult> ReadCharacteristic(string serviceUuid, string characteristicUuid) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(ch => Observable.FromAsync(async ct => 
        {
            //    //this.AssertRead();
            var result = await ch
                .ReadValueAsync(BluetoothCacheMode.Uncached)
                .AsTask(ct)
                .ConfigureAwait(false);

            //    if (result.Status != GattCommunicationStatus.Success)
            //        throw new BleException($"Failed to read characteristic - {result.Status}");

            return new BleCharacteristicResult(
                ToChar(ch),
                BleCharacteristicEvent.Read,
                result.Value?.ToArray()
            );
        }))
        .Switch();


    public IObservable<BleCharacteristicResult> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(ch => Observable.FromAsync(async ct =>
        {
            //this.AssertWrite(withResponse);

            var writeType = withResponse
                ? GattWriteOption.WriteWithResponse
                : GattWriteOption.WriteWithoutResponse;

            await ch
                //.WriteValueAsync(value.AsBuffer(), writeType)
                //.Execute(ct)
                .WriteValueAsync(null, writeType)
                .AsTask(ct)
                .ConfigureAwait(false);

            return new BleCharacteristicResult(
                ToChar(ch),
                BleCharacteristicEvent.Write,
                data
            );
        }))
        .Switch();


    protected static BleCharacteristicInfo ToChar(GattCharacteristic ch) => new BleCharacteristicInfo(
        new BleServiceInfo(ch.Service.Uuid.ToString()),
        ch.Uuid.ToString(),
        false, //ch.ValueChanged != null, 
        CharacteristicProperties.Broadcast
    );


    protected IObservable<GattCharacteristic> GetNativeCharacteristic(string serviceUuid, string characteristicUuid) => Observable.FromAsync(async ct =>
    {
        var suid = Utils.ToUuidType(serviceUuid);

        var result = await this.Native!
            .GetGattServicesForUuidAsync(suid, BluetoothCacheMode.Cached)
            .AsTask(ct)
            .ConfigureAwait(false);

        result.Status.Assert("Get", serviceUuid);
        
        var service = result.Services.FirstOrDefault();
        if (service == null)
            throw new BleException("");

        var cuid = Utils.ToUuidType(characteristicUuid);
        var chResult = await service
            .GetCharacteristicsForUuidAsync(cuid, BluetoothCacheMode.Cached)
            .AsTask(ct)
            .ConfigureAwait(false);

        chResult.Status.Assert("Get", serviceUuid, characteristicUuid);
        var ch = chResult.Characteristics.FirstOrDefault();
        if (ch == null)
            throw new BleException($"No characteristic '{characteristicUuid}' found under service '{serviceUuid}'");

        return ch;
    });
}
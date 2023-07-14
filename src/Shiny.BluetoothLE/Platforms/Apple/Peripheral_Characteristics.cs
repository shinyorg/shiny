using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE.Intrastructure;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    readonly Dictionary<string, IObservable<BleCharacteristicResult>> notifiers = new();
    public IObservable<BleCharacteristicResult> NotifyCharacteristic(string serviceUuid, string characteristicUuid, bool useIndicateIfAvailable = true)
    {
        this.AssertConnnection();
        var key = $"{serviceUuid}-{characteristicUuid}";

        if (!this.notifiers.ContainsKey(key))
        {
            var obs = Observable
                .Create<BleCharacteristicResult>(ob =>
                {
                    this.logger.LogInformation($"Initial Notification Hook: {serviceUuid} / {characteristicUuid}");

                    CBCharacteristic native = null!;
                    this.WhenConnected()
                        .Select(_ =>
                        {
                            this.logger.LogDebug($"Connection Detected - Attempting to hook: {serviceUuid} / {characteristicUuid}");
                            return this.GetNativeCharacteristic(serviceUuid, characteristicUuid);
                        })
                        .Switch()
                        .Select(ch =>
                        {
                            this.FromNative(ch).AssertNotify();

                            native = ch;
                            this.logger.CharacteristicInfo("Hooking Notification Characteristic", serviceUuid, characteristicUuid);
                            this.Native.SetNotifyValue(true, ch);

                            return this.charUpdateSubj
                                .Where(x => x.Char.Equals(ch))
                                .Select(x => this.ToResult(x.Char, BleCharacteristicEvent.Notification));
                        })
                        .Switch()
                        .Subscribe(
                            ob.OnNext,
                            ob.OnError
                        );

                    return () =>
                    {
                        if (native == null)
                            return;

                        try
                        {
                            this.Native.SetNotifyValue(false, native);
                        }
                        catch (Exception ex)
                        {
                            this.logger.DisableNotificationError(ex, serviceUuid, characteristicUuid);
                        }
                    };
                })
                .Publish()
                .RefCount();

            this.notifiers.Add(key, obs);
        }

        return this.notifiers[key];
    }


    public IObservable<BleCharacteristicInfo> GetCharacteristic(string serviceUuid, string characteristicUuid) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(this.FromNative);

    
    public IObservable<BleCharacteristicInfo> WhenCharacteristicSubscriptionChanged(string serviceUuid, string characteristicUuid) => Observable.Create<BleCharacteristicInfo>(ob =>
    {
        var nsUuid = CBUUID.FromString(serviceUuid);
        var ncUuid = CBUUID.FromString(characteristicUuid);

        if (this.Status == ConnectionState.Connected && this.Native.Services != null)
        {
            foreach (var service in this.Native.Services)
            {
                if (service.Characteristics != null)
                {
                    foreach (var ch in service.Characteristics)
                    {
                        if (ch.IsNotifying && ch.Is(nsUuid, ncUuid))
                            ob.OnNext(this.FromNative(ch));
                    }
                }
            }
        }

        return this.notifySubj
            .Where(x => x.Char.Is(nsUuid, ncUuid))
            .Subscribe(x =>
            {
                var info = this.FromNative(x.Char);
                ob.OnNext(info);
            });
    });


    public IObservable<BleCharacteristicResult> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(ch =>
        {
            this.AssertWrite(ch, withResponse);
            return withResponse
                ? this.WriteWithResponse(ch, data)
                : this.WriteWithoutResponse(ch, data);
        })
        .Switch();


    public IObservable<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristics(string serviceUuid) => this
        .GetNativeService(serviceUuid)
        .Select(service => Observable.Create<IReadOnlyList<BleCharacteristicInfo>>(ob =>
        {
            var sub = this.charDiscoverySubj
                .Where(x => x.Service.Equals(service))
                .Subscribe(x =>
                {
                    if (x.Error != null)
                    {
                        ob.OnError(ToException(x.Error));
                    }
                    else if (service.Characteristics != null)
                    {
                        // if existing chars from single discoveries - we likely want second passthrough
                        var list = service.Characteristics.Select(this.FromNative).ToList();
                        ob.Respond(list);
                    }
                });

            this.Native.DiscoverCharacteristics(service);

            return () => sub.Dispose();
        }))
        .Switch();


    public IObservable<BleCharacteristicResult> ReadCharacteristic(string serviceUuid, string characteristicUuid) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(ch => this.operations.QueueToObservable(async ct =>
        {
            if (!ch.Properties.HasFlag(CBCharacteristicProperties.Read))
                throw new InvalidOperationException($"Characteristic '{characteristicUuid}' does not support read");

            var task = this.charUpdateSubj.Where(x => x.Char.Equals(ch)).Take(1).ToTask(ct);
            this.Native.ReadValue(ch);

            var result = await task.ConfigureAwait(false);
            if (result.Error != null)
                throw ToException(result.Error);

            return this.ToResult(ch, BleCharacteristicEvent.Read);
        }))
        .Switch();


    readonly Subject<(CBCharacteristic Char, NSError? Error)> notifySubj = new();
    public override void UpdatedNotificationState(CBPeripheral peripheral, CBCharacteristic characteristic, NSError? error)
    {
        this.logger.CharacteristicNotifyState(characteristic);
        this.notifySubj.OnNext((characteristic, error));
    }

    
    readonly Subject<Unit> readyWwrSubj = new();
    public override void IsReadyToSendWriteWithoutResponse(CBPeripheral peripheral)
    {
        this.logger.LogDebug("IsReadyToSendWriteWithoutResponse Fired");
        this.readyWwrSubj.OnNext(Unit.Default);
    }
    

    readonly Subject<(CBService Service, NSError? Error)> charDiscoverySubj = new();
#if XAMARINIOS
    public override void DiscoveredCharacteristic(CBPeripheral peripheral, CBService service, NSError? error)
#else
    public override void DiscoveredCharacteristics(CBPeripheral peripheral, CBService service, NSError? error)
#endif
    {
        if (this.logger.IsEnabled(LogLevel.Debug))
            this.logger.LogDebug($"[DiscoveredCharacteristics] {service.UUID} - Error: {error?.LocalizedDescription ?? "NONE"}");

        this.charDiscoverySubj.OnNext((service, error));
    }


    readonly Subject<(CBCharacteristic Char, NSError? Error)> charUpdateSubj = new();
    public override void UpdatedCharacterteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError? error)
    {
        this.logger.CharacteristicEvent(characteristic, error);
        this.charUpdateSubj.OnNext((characteristic, error));
    }


    readonly Subject<(CBCharacteristic Char, NSError? Error)> charWroteSubj = new();
    public override void WroteCharacteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError? error)
    {
        this.logger.CharacteristicEvent(characteristic, error);
        this.charWroteSubj.OnNext((characteristic, error));
    }


    protected BleCharacteristicResult ToResult(CBCharacteristic ch, BleCharacteristicEvent @event) => new(
        this.FromNative(ch),
        @event,
        ch.Value?.ToArray()
    );


    protected BleCharacteristicInfo FromNative(CBCharacteristic ch) => new(
        this.FromNative(ch.Service!),
        ch.UUID.ToString(),
        ch.IsNotifying,
        (CharacteristicProperties)ch.Properties
    );


    protected IObservable<CBCharacteristic> GetNativeCharacteristic(string serviceUuid, string characteristicUuid) => this
        .GetNativeService(serviceUuid)
        .Select(service => this.operations.QueueToObservable(async ct => 
        {
            var uuid = CBUUID.FromString(characteristicUuid);
            var ch = service.Characteristics?.FirstOrDefault(x => x.UUID.Equals(uuid));

            if (ch != null)
                return ch;

            var task = this.charDiscoverySubj
                .Where(x => x.Service.Equals(service))
                .Take(1)
                .ToTask(ct);

            this.Native.DiscoverCharacteristics(new[] { uuid }, service);
            var result = await task.ConfigureAwait(false);
            if (result.Error != null)
                throw ToException(result.Error);

            ch = service.Characteristics?.FirstOrDefault(x => x.UUID.Equals(uuid));
            if (ch == null)
                throw new InvalidOperationException($"No characteristic found in service '{serviceUuid}' with UUID: {characteristicUuid}");

            return ch;
        }))
        .Switch();


    protected IObservable<BleCharacteristicResult> WriteWithResponse(CBCharacteristic nativeCh, byte[] value) => this.operations.QueueToObservable(async ct =>
    {
        var data = NSData.FromArray(value);
        var task = this.charWroteSubj.Where(x => x.Char.Equals(nativeCh)).Take(1).ToTask(ct);
        this.Native.WriteValue(data, nativeCh, CBCharacteristicWriteType.WithResponse);

        var result = await task.ConfigureAwait(false);
        if (result.Error != null)
            throw ToException(result.Error);

        return this.ToResult(nativeCh, BleCharacteristicEvent.Write);
    });


    protected IObservable<BleCharacteristicResult> WriteWithoutResponse(CBCharacteristic nativeCh, byte[] value) => this.operations.QueueToObservable(async ct =>
    {
        if (!this.Native.CanSendWriteWithoutResponse)
        {
            this.logger.CanSendWriteWithoutResponse(nativeCh, false);
            await this.readyWwrSubj.Take(1).ToTask(ct);
            this.logger.CanSendWriteWithoutResponse(nativeCh, true);
        }
        this.logger.LogDebug("Writing characteristic without response");
        var data = NSData.FromArray(value);
        this.Native.WriteValue(data, nativeCh, CBCharacteristicWriteType.WithoutResponse);

        return this.ToResult(nativeCh, BleCharacteristicEvent.WriteWithoutResponse);
    });


    protected void AssertWrite(CBCharacteristic ch, bool withResponse)
    {
        if (withResponse && !ch.Properties.HasFlag(CBCharacteristicProperties.Write))
            throw new InvalidOperationException($"Characteristic '{ch.UUID}' does not support write with response");

        if (!withResponse && !ch.Properties.HasFlag(CBCharacteristicProperties.WriteWithoutResponse))
            throw new InvalidOperationException($"Characteristic '{ch.UUID}' does not support write without response");
    }
}
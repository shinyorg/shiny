using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE.Intrastructure;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    IObservable<BleCharacteristicResult>? notifyObservable;
    public IObservable<BleCharacteristicResult> NotifyCharacteristic(string serviceUuid, string characteristicUuid, bool useIndicateIfAvailable = true, bool emitSubscribedEvent = false)
    {
        this.AssertConnnection();

        this.notifyObservable ??= Observable.Create<BleCharacteristicResult>(ob => this
            .WhenConnected()
            .Select(_ => this.GetNativeCharacteristic(serviceUuid, characteristicUuid))
            .Switch()
            .Select(ch => Observable.FromAsync(async ct =>
            {
                this.FromNative(ch).AssertNotify();
                this.logger.LogInformation("Hooking Notification Characteristic: " + characteristicUuid);
                var task = Task.CompletedTask;

                if (emitSubscribedEvent)
                {
                    task = this.notifySubj
                        .Take(1)
                        .Where(x => x.Char.Equals(ch))
                        .ToTask(ct);
                }
                this.Native.SetNotifyValue(true, ch);
                await task.ConfigureAwait(false);

                if (emitSubscribedEvent)
                    ob.OnNext(this.ToResult(ch, BleCharacteristicEvent.NotificationSubscribed));

                return ch;
            }))
            .Switch()
            .Select(ch => this.charUpdateSubj
                .Where(x => x.Char.Equals(ch))
                .Select(x => this.ToResult(x.Char, BleCharacteristicEvent.Notification))
                .Finally(() =>
                {
                    try
                    {
                        this.Native.SetNotifyValue(false, ch);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogWarning("Unable to cleanly dispose of characteristic notifications", ex);
                    }
                })
            )
            .Switch()
            .Subscribe(
                ob.OnNext,
                ob.OnError
            )
        )
        .Publish()
        .RefCount();

        return this.notifyObservable;
    }


    public IObservable<BleCharacteristicInfo> GetCharacteristic(string serviceUuid, string characteristicUuid) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(this.FromNative);


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
                        ob.OnError(new InvalidOperationException(x.Error.LocalizedDescription));
                    }
                    else if (service.Characteristics != null)
                    {
                        // TODO: if existing chars from single discoveries - we likely want second passthrough
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
                throw new InvalidCastException(result.Error.LocalizedDescription);

            return this.ToResult(ch, BleCharacteristicEvent.Read);
        }))
        .Switch();


    readonly Subject<(CBCharacteristic Char, NSError? Error)> notifySubj = new();
    public override void UpdatedNotificationState(CBPeripheral peripheral, CBCharacteristic characteristic, NSError? error)
        => this.notifySubj.OnNext((characteristic, error));

    readonly Subject<Unit> readyWwrSubj = new();
    public override void IsReadyToSendWriteWithoutResponse(CBPeripheral peripheral)
        => this.readyWwrSubj.OnNext(Unit.Default);

    readonly Subject<(CBService Service, NSError? Error)> charDiscoverySubj = new();
#if XAMARINIOS
    public override void DiscoveredCharacteristic(CBPeripheral peripheral, CBService service, NSError? error)
#else
    public override void DiscoveredCharacteristics(CBPeripheral peripheral, CBService service, NSError? error)
#endif
        => this.charDiscoverySubj.OnNext((service, error));


    readonly Subject<(CBCharacteristic Char, NSError? Error)> charUpdateSubj = new();
    public override void UpdatedCharacterteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError? error)
        => this.charUpdateSubj.OnNext((characteristic, error));


    readonly Subject<(CBCharacteristic Char, NSError? Error)> charWroteSubj = new();
    public override void WroteCharacteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError? error)
        => this.charWroteSubj.OnNext((characteristic, error));


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
        .Select(service => Observable.Create<CBCharacteristic>(ob =>
        {
            IDisposable? sub = null;
            var uuid = CBUUID.FromString(characteristicUuid);
            var ch = service.Characteristics?.FirstOrDefault(x => x.UUID.Equals(uuid));

            if (ch != null)
            {
                ob.Respond(ch);
            }
            else
            {
                var count = 0;

                sub = this.charDiscoverySubj
                    .Where(x => x.Service.Equals(service))
                    .Subscribe(x =>
                    {
                        count++;
                        if (x.Error != null)
                        {
                            ob.OnError(new InvalidOperationException(x.Error.LocalizedDescription));
                        }
                        else
                        {
                            ch = service.Characteristics?.FirstOrDefault(x => x.UUID.Equals(uuid));
                            if (ch != null)
                            {
                                ob.Respond(ch);
                            }
                            else if (count >= 2)
                            { 
                                ob.OnError(new InvalidOperationException($"No characteristic found in service '{serviceUuid}' with UUID: {characteristicUuid}"));
                            }
                        }
                    });

                this.Native.DiscoverCharacteristics(new[] { uuid }, service);
            }

            return () => sub?.Dispose();
        }))
        .Switch();


    protected IObservable<BleCharacteristicResult> WriteWithResponse(CBCharacteristic nativeCh, byte[] value) => this.operations.QueueToObservable(async ct =>
    {
        var data = NSData.FromArray(value);
        var task = this.charWroteSubj.Where(x => x.Char.Equals(nativeCh)).Take(1).ToTask(ct);
        this.Native.WriteValue(data, nativeCh, CBCharacteristicWriteType.WithResponse);

        var result = await task.ConfigureAwait(false);
        //result.Error.Code == CBError.PeripheralDisconnected
        if (result.Error != null)
            throw new BleException(result.Error.LocalizedDescription);

        return this.ToResult(nativeCh, BleCharacteristicEvent.Write);
    });


    protected IObservable<BleCharacteristicResult> WriteWithoutResponse(CBCharacteristic nativeCh, byte[] value) => this.operations.QueueToObservable(async ct =>
    {
        if (!this.Native.CanSendWriteWithoutResponse)
        {
            this.logger.LogDebug("Waiting for 'CanSendWriteWithoutResponse'");
            await this.readyWwrSubj.Take(1).ToTask(ct);
            this.logger.LogDebug("'CanSendWriteWithoutResponse' is ready");
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
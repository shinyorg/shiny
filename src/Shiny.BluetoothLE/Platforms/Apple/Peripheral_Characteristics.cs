using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE.Intrastructure;
using Shiny.BluetoothLE.Infrastructure;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    // Not cleared on disconnect: each stream re-hooks itself on WhenConnected, so dropping it from the cache let a later
    // caller build a second stream for the same characteristic, whose disposal turned notifications off under the first.
    readonly Dictionary<string, IObservable<BleCharacteristicResult>> notifiers = new();

    public IObservable<BleCharacteristicResult> NotifyCharacteristic(string serviceUuid, string characteristicUuid, bool useIndicateIfAvailable = true)
    {
        this.AssertConnnection();
        var key = $"{serviceUuid}-{characteristicUuid}";

        lock (this.notifiers)
        {
            if (this.notifiers.TryGetValue(key, out var existing))
                return existing;

            IObservable<BleCharacteristicResult>? obs = null;
            obs = Observable
                .Create<BleCharacteristicResult>(ob =>
                {
                    this.logger.LogInformation($"Initial Notification Hook: {serviceUuid} / {characteristicUuid}");

                    CBCharacteristic native = null!;
                    var sub = this.WhenConnected()
                        .Select(_ =>
                        {
                            this.logger.LogDebug($"Connection Detected - Attempting to hook: {serviceUuid} / {characteristicUuid}");
                            return this.GetNativeCharacteristic(serviceUuid, characteristicUuid);
                        })
                        .Switch()
                        .Select(ch =>
                        {
                            native = ch;
                            this.FromNative(ch).AssertNotify();

                            // Live notification stream. Merge subscribes to this FIRST (below), so the
                            // earliest UpdatedCharacterteristicValue callbacks are captured before
                            // SetNotifyValue turns the peripheral's notifications on - devices that
                            // stream immediately on subscribe would otherwise drop them (issue #1542).
                            var notifyStream = this.charUpdateSubj
                                .Where(x => x.Char.Equals(ch) && x.Error == null)
                                .Select(x => this.ToResult(x.Char, BleCharacteristicEvent.Notification, x.Data));

                            // SetNotifyValue reports through UpdatedNotificationState - a peripheral refusing the
                            // subscription (e.g. insufficient authentication) errors the stream instead of leaving it
                            // silent forever. Subscribed before the call so the callback can't be missed.
                            var enable = Observable.Create<BleCharacteristicResult>(eob =>
                            {
                                var stateSub = this.notifySubj
                                    .Where(x => x.Char.Equals(ch))
                                    .Take(1)
                                    .Subscribe(x =>
                                    {
                                        if (x.Error == null)
                                            eob.OnCompleted();
                                        else
                                            eob.OnError(ToException(x.Error));
                                    });

                                this.logger.CharacteristicInfo("Hooking Notification Characteristic", serviceUuid, characteristicUuid);
                                this.Native.SetNotifyValue(true, ch);
                                return stateSub;
                            });

                            return notifyStream.Merge(enable);
                        })
                        .Switch()
                        .Subscribe(
                            ob.OnNext,
                            ob.OnError
                        );

                    return () =>
                    {
                        sub?.Dispose();
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
                // drop the cached stream once it ends - an errored one would otherwise replay its error to every later
                // subscriber, and after the last subscriber leaves the next caller should start a fresh one
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


    // Queued like the single-characteristic discovery in GetNativeCharacteristic: run alongside it, either one's
    // DiscoveredCharacteristics callback completed the other (a full listing came back with one characteristic), and
    // outside WaitForOperation a disconnect mid-discovery left it waiting forever.
    public IObservable<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristics(string serviceUuid) => this
        .GetNativeService(serviceUuid)
        .Select(service => this.operations.QueueToObservable(async ct =>
        {
            var task = this.WaitForOperation(this.charDiscoverySubj.Where(x => x.Service.Equals(service)), ct);
            this.Native.DiscoverCharacteristics(service);

            var result = await task.ConfigureAwait(false);
            if (result.Error != null)
                throw ToException(result.Error);

            IReadOnlyList<BleCharacteristicInfo> list = service.Characteristics?.Select(this.FromNative).ToList() ?? new List<BleCharacteristicInfo>();
            return list;
        }))
        .Switch();


    public IObservable<BleCharacteristicResult> ReadCharacteristic(string serviceUuid, string characteristicUuid) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(ch => this.operations.QueueToObservable(async ct =>
        {
            if (!ch.Properties.HasFlag(CBCharacteristicProperties.Read))
                throw new InvalidOperationException($"Characteristic '{characteristicUuid}' does not support read");

            var task = this.WaitForOperation(this.charUpdateSubj.Where(x => x.Char.Equals(ch)), ct);
            this.Native.ReadValue(ch);

            var result = await task.ConfigureAwait(false);
            if (result.Error != null)
                throw ToException(result.Error);

            // the value captured in the callback - ch.Value can already hold a later notification by now
            return this.ToResult(ch, BleCharacteristicEvent.Read, result.Data);
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


    // reads and notifications both arrive here - the value is copied now, before the next update overwrites it
    readonly Subject<(CBCharacteristic Char, NSError? Error, byte[]? Data)> charUpdateSubj = new();
    public override void UpdatedCharacterteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError? error)
    {
        this.logger.CharacteristicEvent(characteristic, error);
        this.charUpdateSubj.OnNext((characteristic, error, error == null ? characteristic.Value?.ToArray() : null));
    }


    readonly Subject<(CBCharacteristic Char, NSError? Error)> charWroteSubj = new();
    public override void WroteCharacteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError? error)
    {
        this.logger.CharacteristicEvent(characteristic, error);
        this.charWroteSubj.OnNext((characteristic, error));
    }


    protected BleCharacteristicResult ToResult(CBCharacteristic ch, BleCharacteristicEvent @event, byte[]? data) => new(
        this.FromNative(ch),
        @event,
        data
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

            var task = this.WaitForOperation(
                this.charDiscoverySubj.Where(x => x.Service.Equals(service)),
                ct
            );

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
        var task = this.WaitForOperation(this.charWroteSubj.Where(x => x.Char.Equals(nativeCh)), ct);
        this.Native.WriteValue(data, nativeCh, CBCharacteristicWriteType.WithResponse);

        var result = await task.ConfigureAwait(false);
        if (result.Error != null)
            throw ToException(result.Error);

        // CoreBluetooth never stores a written value on the characteristic - ch.Value is the last read/notification
        return this.ToResult(nativeCh, BleCharacteristicEvent.Write, value);
    });


    // Write without response runs in its own lane: the CanSendWriteWithoutResponse wait happens here, outside the
    // global operation queue, so a peripheral that stops draining its buffer only stalls these writes - not every
    // GATT operation in the process (#1672). The operation lock is only taken for the WriteValue call itself.
    readonly SemaphoreSlim wwrLane = new(1);
    readonly Subject<Unit> wwrRecheckSubj = new();

    // a peripheral that never drains its buffer fails the write rather than parking the lane until disconnect - callers
    // wanting less dispose/cancel sooner
    static readonly TimeSpan WriteWithoutResponseReadyTimeout = TimeSpan.FromSeconds(5);

    protected IObservable<BleCharacteristicResult> WriteWithoutResponse(CBCharacteristic nativeCh, byte[] value) => ObservableTask.FromAsync(async ct =>
    {
        // CoreBluetooth can flip CanSendWriteWithoutResponse back to true without calling IsReadyToSendWriteWithoutResponse
        // (seen around app suspension), so every new write makes a parked write look at the flag again
        this.wwrRecheckSubj.OnNext(Unit.Default);

        await this.wwrLane.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await this.WaitForCanSendWriteWithoutResponse(nativeCh, ct).ConfigureAwait(false);

            // disposing before this point cancels the write - once WriteValue is called, it is with CoreBluetooth
            BleCharacteristicResult result = null!;
            await this.operations.Queue(() =>
            {
                this.logger.LogDebug("Writing characteristic without response");
                this.Native.WriteValue(NSData.FromArray(value), nativeCh, CBCharacteristicWriteType.WithoutResponse);
                result = this.ToResult(nativeCh, BleCharacteristicEvent.WriteWithoutResponse, value);
                return Task.CompletedTask;
            }, ct, nameof(WriteWithoutResponse)).ConfigureAwait(false);

            return result;
        }
        finally
        {
            this.wwrLane.Release();
        }
    });


    async Task WaitForCanSendWriteWithoutResponse(CBCharacteristic nativeCh, CancellationToken ct)
    {
        if (this.Native.CanSendWriteWithoutResponse)
            return;

        this.logger.CanSendWriteWithoutResponse(nativeCh, false);

        // readyWwrSubj is hot and IsReadyToSendWriteWithoutResponse fires once, on CoreBluetooth's queue. Merge
        // subscribes to it BEFORE the deferred re-check runs, so a callback that lands between the check above and
        // the wait is still seen (#1657)
        var ready = this.readyWwrSubj.Merge(this.wwrRecheckSubj
            .Merge(Observable.Defer(() => Observable.Return(Unit.Default)))
            .Where(_ => this.Native.CanSendWriteWithoutResponse)
        );

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(WriteWithoutResponseReadyTimeout);
        try
        {
            await this.WaitForOperation(ready, timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new BleException($"Peripheral was not ready to send write without response within {WriteWithoutResponseReadyTimeout.TotalSeconds} seconds (CanSendWriteWithoutResponse stayed false)");
        }
        this.logger.CanSendWriteWithoutResponse(nativeCh, true);
    }


    protected void AssertWrite(CBCharacteristic ch, bool withResponse)
    {
        if (withResponse && !ch.Properties.HasFlag(CBCharacteristicProperties.Write))
            throw new InvalidOperationException($"Characteristic '{ch.UUID}' does not support write with response");

        if (!withResponse && !ch.Properties.HasFlag(CBCharacteristicProperties.WriteWithoutResponse))
            throw new InvalidOperationException($"Characteristic '{ch.UUID}' does not support write without response");
    }
}
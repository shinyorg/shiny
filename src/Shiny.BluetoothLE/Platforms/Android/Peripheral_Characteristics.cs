using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Android.Bluetooth;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE.Intrastructure;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    public const string NotifyDescriptorUuid = "00002902-0000-1000-8000-00805f9b34fb";    

    public IObservable<BleCharacteristicInfo> GetCharacteristic(string serviceUuid, string characteristicUuid) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(this.FromNative);

    
    public IObservable<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristics(string serviceUuid) => this
        .GetNativeService(serviceUuid)
        .Select(service => service
            .Characteristics!
            .Select(this.FromNative)
            .ToList()
        );

    
    public IObservable<BleCharacteristicResult> ReadCharacteristic(string serviceUuid, string characteristicUuid) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(ch => this.operations.QueueToObservable(async ct => 
        {
            this.charEventSubj ??= new();

            this.FromNative(ch).AssertRead();
            var task = this.charEventSubj
                .Where(x => x.Char.Equals(ch) && !x.IsWrite)
                .Take(1)
                .ToTask(ct);

            if (!this.Gatt!.ReadCharacteristic(ch))
                throw new BleException("Failed to read characteristic: " + characteristicUuid);

            var result = await task.ConfigureAwait(false);
            if (result.Status != GattStatus.Success)
                throw ToException("Failed to read characteristic: " + characteristicUuid, result.Status);

            return this.ToResult(ch, BleCharacteristicEvent.Read);
        }))
        .Switch();


    Subject<BleCharacteristicInfo>? charSubSubj;
    Dictionary<string, IObservable<BleCharacteristicResult>>? notifiers;
    public IObservable<BleCharacteristicResult> NotifyCharacteristic(string serviceUuid, string characteristicUuid, bool useIndicationsIfAvailable = true)
    {
        this.AssertConnection();

        this.notifiers ??= new();
        this.notifySubj ??= new();
        this.charSubSubj ??= new();

        var key = $"{serviceUuid}-{characteristicUuid}";

        if (!this.notifiers.ContainsKey(key))
        {
            var obs = Observable
                .Create<BleCharacteristicResult>(ob =>
                {
                    this.logger.LogDebug($"Hooking Characteristic Notification: {serviceUuid} / {characteristicUuid}");
                    BluetoothGattCharacteristic? characteristic = null;

                    var sub = this.WhenConnected()
                        .Select(_ =>
                        {
                            this.logger.LogDebug($"Connection Detected - Attempting to hook characteristic: {serviceUuid} / {characteristicUuid}");
                            return this.GetNativeCharacteristic(serviceUuid, characteristicUuid);
                        })
                        .Switch()
                        .Select(ch => this.operations.QueueToObservable(async ct =>
                        {
                            this.FromNative(ch).AssertNotify();
                            this.logger.LogDebug("Char InstanceID: " + ch.InstanceId);

                            this.logger.HookedCharacteristic(serviceUuid, characteristicUuid, "Subscribing");

                            if (!this.Gatt!.SetCharacteristicNotification(ch, true))
                                throw new BleException("Failed to set characteristic notification value");

                            var notifyBytes = this.GetNotifyDescriptorBytes(ch, useIndicationsIfAvailable);
                            var nativeDescriptor = ch.GetDescriptor(Utils.ToUuidType(NotifyDescriptorUuid));
                            if (nativeDescriptor == null)
                                throw new BleException("Characteristic notification descriptor not found");

                            await this.WriteDescriptor(nativeDescriptor, notifyBytes, ct).ConfigureAwait(false);
                            this.logger.HookedCharacteristic(serviceUuid, characteristicUuid, "Subscribed");

                            this.AddNotify(ch);
                            this.charSubSubj.OnNext(this.FromNative(ch));

                            characteristic = ch;
                            return ch;
                        }))
                        .Switch()
                        .Select(ch => this.notifySubj
                            .Where(x => x.Equals(ch))
                            .Select(x => this.ToResult(x, BleCharacteristicEvent.Notification))
                        )
                        .Switch()
                        .Subscribe(
                            ob.OnNext,
                            ob.OnError
                        );

                    return () =>
                    {
                        if (characteristic != null)
                            this.TryNotificationCleanup(characteristic, serviceUuid, characteristicUuid);

                        sub?.Dispose();
                    };
                })
                .Publish()
                .RefCount();

            this.notifiers.Add(key, obs);
        }

        return this.notifiers[key];
    }


    public IObservable<BleCharacteristicInfo> WhenCharacteristicSubscriptionChanged(string serviceUuid, string characteristicUuid) => Observable.Create<BleCharacteristicInfo>(ob =>
    {
        this.charSubSubj ??= new();

        if (this.Status == ConnectionState.Connected &&
            !this.RequiresServiceDiscovery &&
            this.Gatt!.Services != null)
        {
            var ns = Utils.ToUuidType(serviceUuid);
            var nc = Utils.ToUuidType(characteristicUuid);

            foreach (var service in this.Gatt!.Services)
            {
                if (service.Characteristics != null)
                {
                    foreach (var ch in service.Characteristics)
                    {
                        if (ch.Is(ns, nc) && this.IsNotifying(ch))
                        {
                            var info = this.FromNative(ch);
                            ob.OnNext(info);
                        }
                    }
                }
            }
        }

        return this.charSubSubj.Subscribe(ob.OnNext);
    });


    protected void TryNotificationCleanup(BluetoothGattCharacteristic ch, string serviceUuid, string characteristicUuid)
    {
        try
        {            
            this.RemoveNotify(ch);

            if (this.Status == ConnectionState.Connected)
            {
                this.WriteDescriptor(
                        serviceUuid,
                        characteristicUuid,
                        NotifyDescriptorUuid,
                        BluetoothGattDescriptor.DisableNotificationValue!.ToArray()
                    )
                    .Timeout(TimeSpan.FromSeconds(3))
                    .Subscribe(
                        _ => { },
                        ex => this.logger.DisableNotificationError(ex, serviceUuid, characteristicUuid)
                    );

                if (!this.Gatt!.SetCharacteristicNotification(ch, false))
                    this.logger.DisableNotificationError(null!, serviceUuid, characteristicUuid);
                
                this.charSubSubj?.OnNext(this.FromNative(ch));
            }
            this.logger.LogDebug($"Cleaned up characteristic subscription: {serviceUuid} / {characteristicUuid}");
        }
        catch (Exception ex)
        {
            this.logger.DisableNotificationError(ex, serviceUuid, characteristicUuid);
        }
    }


    public IObservable<BleCharacteristicResult> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(ch => this.operations.QueueToObservable(async ct =>
        {
            this.FromNative(ch).AssertWrite(withResponse);

            this.charEventSubj ??= new();
            var task = this.charEventSubj
                .Where(x => x.Char.Equals(ch) && x.IsWrite)
                .Take(1)
                .ToTask(ct);

            ch.WriteType = withResponse ? GattWriteType.Default : GattWriteType.NoResponse;
            
            if (ch.Properties.HasFlag(GattProperty.SignedWrite) && this.Native.BondState == Bond.Bonded)
                ch.WriteType |= GattWriteType.Signed;

#if XAMARIN
            if (!ch.SetValue(data))
                throw new BleException("Could not set value of characteristic: " + characteristicUuid);

            if (!this.Gatt!.WriteCharacteristic(ch))
                throw new BleException("Failed to write to characteristic: " + characteristicUuid);
#else
            if (OperatingSystemShim.IsAndroidVersionAtLeast(33))
            {
                this.Gatt!.WriteCharacteristic(ch, data, (int)ch.WriteType);
            }
            else
            {
                if (!ch.SetValue(data))
                    throw new BleException("Could not set value of characteristic: " + characteristicUuid);

                if (!this.Gatt!.WriteCharacteristic(ch))
                    throw new BleException("Failed to write to characteristic: " + characteristicUuid);
            }
#endif
            var result = await task.ConfigureAwait(false);
            if (result.Status != GattStatus.Success)
                throw ToException($"Failed to write to characteristic: {characteristicUuid}", result.Status);

            var eventType = withResponse
                ? BleCharacteristicEvent.Write
                : BleCharacteristicEvent.WriteWithoutResponse;

            return this.ToResult(ch, eventType);
        }))
        .Switch();



    protected IObservable<BluetoothGattCharacteristic> GetNativeCharacteristic(string serviceUuid, string characteristicUuid) => this
        .GetNativeService(serviceUuid)
        .Select(service =>
        {
            var uuid = Utils.ToUuidType(characteristicUuid);
            var ch = service.Characteristics!.FirstOrDefault(x => x.Uuid.Equals(uuid));
            if (ch == null)
                throw new InvalidOperationException($"No characteristic '{characteristicUuid}' exists under service '{serviceUuid}'");

            return ch;
        });

    
    protected BleCharacteristicResult ToResult(BluetoothGattCharacteristic ch, BleCharacteristicEvent @event) => new BleCharacteristicResult(
        this.FromNative(ch),
        @event,
        ch.GetValue()
    );

    
    protected BleCharacteristicInfo FromNative(BluetoothGattCharacteristic ch) => new BleCharacteristicInfo(
        this.FromNative(ch.Service!),
        ch.Uuid!.ToString(),
        this.IsNotifying(ch),
        (CharacteristicProperties)(int)ch.Properties
    );
    
    
    List<string>? notifications;
    protected bool IsNotifying(BluetoothGattCharacteristic native)
    {
        this.notifications ??= new();
        lock (this.notifications)
            return this.notifications.Contains(this.NotifyKey(native));
    }


    protected void AddNotify(BluetoothGattCharacteristic native)
    {
        this.notifications ??= new();
        lock (this.notifications)
            this.notifications.Add(this.NotifyKey(native));
    }


    protected void RemoveNotify(BluetoothGattCharacteristic native)
    {
        this.notifications ??= new();
        lock (this.notifications)
            this.notifications.Remove(this.NotifyKey(native));
    }


    protected void ClearNotifications()
    {
        if (this.notifications != null)
        {
            lock (this.notifications)
                this.notifications.Clear();
        }
    }


    protected string NotifyKey(BluetoothGattCharacteristic native)
    {
        if (native?.Service == null)
            throw new ArgumentException("Invalid Characteristic - Service is not set");

        return $"{native.Service.Uuid}-{native.Uuid}".ToLower();
    }


    protected byte[] GetNotifyDescriptorBytes(BluetoothGattCharacteristic ch, bool useIndicationsIfAvailable)
    {
        if (useIndicationsIfAvailable && ch.Properties.HasFlag(GattProperty.Indicate))
            return BluetoothGattDescriptor.EnableIndicationValue!.ToArray();

        return BluetoothGattDescriptor.EnableNotificationValue!.ToArray();
    }


    Subject<(BluetoothGattCharacteristic Char, GattStatus Status, bool IsWrite)>? charEventSubj;
    public override void OnCharacteristicRead(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status)
    {
        this.logger.CharacteristicEvent(characteristic, status);
        this.charEventSubj?.OnNext((characteristic!, status, false));
    }


    public override void OnCharacteristicWrite(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status)
    {
        this.logger.CharacteristicEvent(characteristic, status);
        this.charEventSubj?.OnNext((characteristic!, status, true));
    }


    Subject<BluetoothGattCharacteristic>? notifySubj = new();
    public override void OnCharacteristicChanged(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic)
    {
        this.logger.CharacteristicEvent(characteristic, null);
        this.notifySubj?.OnNext(characteristic!);
    }
}
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
            this.FromNative(ch).AssertRead();
            var task = this.charEventSubj
                .Where(x => x.Char.Equals(ch) && !x.IsWrite)
                .Take(1)
                .ToTask(ct);

            if (!this.Gatt!.ReadCharacteristic(ch))
                throw new BleException("Failed to read characteristic: " + characteristicUuid);

            var result = await task.ConfigureAwait(false);
            if (result.Status != GattStatus.Success)
                throw new BleException("Failed to read characteristic: " + result.Status);

            return this.ToResult(ch, BleCharacteristicEvent.Read);
        }))
        .Switch();

    
    public IObservable<BleCharacteristicInfo> WhenCharacteristicSubscriptionChanged() => Observable.Create<BleCharacteristicInfo>(ob =>
    {
        this.AssertConnection();

        if (this.Gatt!.Services != null)
        {
            // TODO: safety lock the notication queue
            foreach (var service in this.Gatt!.Services)
            {
                if (service.Characteristics != null)
                {
                    foreach (var ch in service.Characteristics)
                    {
                        if (this.IsNotifying(ch.Service!.Uuid!.ToString(), ch.Uuid!.ToString()))
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
    
    readonly Subject<BleCharacteristicInfo> charSubSubj = new();
    IObservable<BleCharacteristicResult>? notifyObs;
    public IObservable<BleCharacteristicResult> NotifyCharacteristic(string serviceUuid, string characteristicUuid, bool useIndicationsIfAvailable = true)
    {
        this.AssertConnection();
        
        this.notifyObs ??= this
            .WhenConnected()
            .Select(_ => this.GetNativeCharacteristic(serviceUuid, characteristicUuid))
            .Switch()
            .Select(ch => this.operations.QueueToObservable(async ct =>
            {
                this.FromNative(ch).AssertNotify();

                this.logger.LogInformation("Hook Notification Characteristic: " + characteristicUuid);

                if (!this.Gatt!.SetCharacteristicNotification(ch, true))
                    throw new BleException("Failed to set characteristic notification value");

                var notifyBytes = this.GetNotifyDescriptorBytes(ch, useIndicationsIfAvailable);
                var nativeDescriptor = ch.GetDescriptor(Utils.ToUuidType(NotifyDescriptorUuid));
                if (nativeDescriptor == null)
                    throw new BleException("Characteristic notification descriptor not found");

                await this.WriteDescriptor(nativeDescriptor, notifyBytes, ct).ConfigureAwait(false);
                this.logger.LogInformation($"Hooked Notification Characteristic '{characteristicUuid}' successfully");

                this.AddNotify(serviceUuid, characteristicUuid);
                this.charSubSubj.OnNext(this.FromNative(ch));
                
                return ch;
            }))
            .Switch()
            .Select(ch => this.notifySubj
                .Where(x => x.Equals(ch))
                .Select(x => this.ToResult(x, BleCharacteristicEvent.Notification))
                .Finally(() => this.TryNotificationCleanup(ch, serviceUuid, characteristicUuid))
            )
            .Switch()
            .Publish()
            .RefCount();

        return this.notifyObs!;
    }


    protected void TryNotificationCleanup(BluetoothGattCharacteristic ch, string serviceUuid, string characteristicUuid)
    {
        try
        {
            this.RemoveNotify(serviceUuid, characteristicUuid);

            if (this.Status == ConnectionState.Connected)
            {
                // TODO: is this actually needed?
                this.WriteDescriptor(
                    serviceUuid,
                    characteristicUuid,
                    NotifyDescriptorUuid,
                    BluetoothGattDescriptor.DisableNotificationValue!.ToArray()
                )
                .Subscribe(
                    _ => { },
                    ex => this.logger.LogWarning(ex, "Could not cleanly disable notifications on characteristic: " + characteristicUuid)
                );

                if (!this.Gatt!.SetCharacteristicNotification(ch, false))
                    this.logger.LogWarning("Could not cleanly disable notifications on characteristic: " + characteristicUuid);
                
                this.charSubSubj.OnNext(this.FromNative(ch));
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Error disabling notifications on characteristic: " + characteristicUuid);
        }
    }


    public IObservable<BleCharacteristicResult> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(ch => this.operations.QueueToObservable(async ct =>
        {
            this.FromNative(ch).AssertWrite(withResponse);
            var task = this.charEventSubj.Where(x => x.Char.Equals(ch) && x.IsWrite).Take(1).ToTask(ct);

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
                throw new BleException($"Failed to write to characteristic: {characteristicUuid} - {result.Status}");

            var eventType = withResponse
                ? BleCharacteristicEvent.Write
                : BleCharacteristicEvent.WriteWithoutResponse;

            return this.ToResult(ch, eventType);
        }))
        .Switch();


    readonly Subject<(BluetoothGattCharacteristic Char, GattStatus Status, bool IsWrite)> charEventSubj = new();
    public override void OnCharacteristicRead(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status)
        => this.charEventSubj.OnNext((characteristic!, status, false));

    public override void OnCharacteristicWrite(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status)
        => this.charEventSubj.OnNext((characteristic!, status, true));

    readonly Subject<BluetoothGattCharacteristic> notifySubj = new();
    public override void OnCharacteristicChanged(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic)
        => this.notifySubj.OnNext(characteristic!);


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
        this.FromNative(ch.Service),
        ch.Uuid.ToString(),
        this.IsNotifying(ch.Service.Uuid.ToString(), ch.Uuid.ToString()),
        (CharacteristicProperties)(int)ch.Properties
    );
    
    
    readonly List<string> notifications = new();
    protected bool IsNotifying(string serviceUuid, string characteristicUuid)
        => this.notifications.Contains($"{serviceUuid}-{characteristicUuid}".ToLower());

    protected void AddNotify(string serviceUuid, string characteristicUuid)
        => this.notifications.Add($"{serviceUuid}-{characteristicUuid}".ToLower());

    protected void RemoveNotify(string serviceUuid, string characteristicUuid)
        => this.notifications.Remove($"{serviceUuid}-{characteristicUuid}".ToLower());

    protected byte[] GetNotifyDescriptorBytes(BluetoothGattCharacteristic ch, bool useIndicationsIfAvailable)
    {
        if (useIndicationsIfAvailable && ch.Properties.HasFlag(GattProperty.Indicate))
            return BluetoothGattDescriptor.EnableIndicationValue.ToArray();

        return BluetoothGattDescriptor.EnableNotificationValue.ToArray();
    }
}
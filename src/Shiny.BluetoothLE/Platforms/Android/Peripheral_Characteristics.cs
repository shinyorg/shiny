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
            if (!ch.Properties.HasFlag(GattProperty.Read))
                throw new InvalidOperationException($"Characteristic '{characteristicUuid}' does not support read");

            var task = this.charEventSubj
                .Where(x => x.Char.Equals(ch) && !x.IsWrite)
                .Take(1)
                .ToTask();

            if (!this.Gatt!.ReadCharacteristic(ch))
                throw new BleException("Failed to read characteristic: " + characteristicUuid);

            var result = await task.ConfigureAwait(false);
            if (result.Status != GattStatus.Success)
                throw new BleException("Failed to read characteristic: " + result.Status);

            return this.ToResult(ch, BleCharacteristicEvent.Read);
        }))
        .Switch();

    IObservable<BleCharacteristicResult>? notifyObs;
    public IObservable<BleCharacteristicResult> NotifyCharacteristic(string serviceUuid, string characteristicUuid, bool useIndicationsIfAvailable = true, bool autoReconnect = true)
    {
        this.notifyObs ??= this.GetNativeCharacteristic(serviceUuid, characteristicUuid)
            .Select(ch => this.operations.QueueToObservable(async ct =>
            {
                // TODO: finish auto reconnect
                this.FromNative(ch).AssertNotify();

                this.logger.LogInformation("Hook Notification Characteristic: " + characteristicUuid);

                if (!this.Gatt.SetCharacteristicNotification(ch, true))
                    throw new BleException("Failed to set characteristic notification value");

                // TODO: writedescriptor will incur inside of another lock, thus causing a deadlock - we need a safe write
                var notifyBytes = this.GetNotifyDescriptorBytes(ch, useIndicationsIfAvailable);
                await this.WriteDescriptorAsync(serviceUuid, characteristicUuid, NotifyDescriptorUuid, notifyBytes);
                this.logger.LogInformation($"Hooked Notification Characteristic '{characteristicUuid}' successfully");

                this.AddNotify(serviceUuid, characteristicUuid);
                return ch;
            }))
            .Switch()
            .Select(ch => this.notifySubj
                .Where(x => x.Equals(ch))
                .Select(x => this.ToResult(x, BleCharacteristicEvent.Notification))
                .Finally(() =>
                {
                    try
                    {
                        this.RemoveNotify(serviceUuid, characteristicUuid);

                        this.WriteDescriptor(
                            serviceUuid,
                            characteristicUuid,
                            NotifyDescriptorUuid,
                            BluetoothGattDescriptor.DisableNotificationValue.ToArray()
                        )
                        .Subscribe(
                            _ => { },
                            ex => this.logger.LogWarning(ex, "Could not cleanly disable notifications on characteristic: " + characteristicUuid)
                        );

                        if (!this.Gatt.SetCharacteristicNotification(ch, false))
                            this.logger.LogWarning("Could not cleanly disable notifications on characteristic: " + characteristicUuid);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogWarning(ex, "Error disabling notifications on characteristic: " + characteristicUuid);
                    }
                })
            )
            .Switch()
            .Publish()
            .RefCount();

        return this.notifyObs!;
    }


    public IObservable<BleCharacteristicResult> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(ch => this.operations.QueueToObservable(async ct =>
        {
            // TODO: assert write
            var task = this.charEventSubj.Where(x => x.Char.Equals(ch) && x.IsWrite).Take(1).ToTask(ct);

            ch.WriteType = withResponse ? GattWriteType.Default : GattWriteType.NoResponse;
            
            if (ch.Properties.HasFlag(GattProperty.SignedWrite) && this.Native.BondState == Bond.Bonded)
                ch.WriteType |= GattWriteType.Signed;

            if (!ch.SetValue(data))
                throw new BleException("Could not set value of characteristic: " + characteristicUuid);

            if (!this.Gatt!.WriteCharacteristic(ch))
                throw new BleException("Failed to write to characteristic: " + characteristicUuid);
            
            var result = await task.ConfigureAwait(false);
            if (result.Status != GattStatus.Success)
                throw new BleException("Failed to write to characteristic: " + characteristicUuid);

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
        if (useIndicationsIfAvailable || (!ch.Properties.HasFlag(GattProperty.Notify) && ch.Properties.HasFlag(GattProperty.Indicate)))
            return BluetoothGattDescriptor.EnableIndicationValue.ToArray();

        return BluetoothGattDescriptor.EnableNotificationValue.ToArray();
    }


    protected void AssertWrite(BluetoothGattCharacteristic ch, bool withResponse)
    {
        if (withResponse && !ch.Properties.HasFlag(GattProperty.Write))
            throw new InvalidOperationException($"Characteristic '{ch.Uuid}' does not support write with response");

        if (!withResponse && !ch.Properties.HasFlag(GattProperty.WriteNoResponse))
            throw new InvalidOperationException($"Characteristic '{ch.Uuid}' does not support write without response");
    }
}
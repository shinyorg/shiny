using System;
using System.Reactive.Subjects;
using Android.Bluetooth;


namespace Shiny.BluetoothLE.Internals
{
    public class GattCallbacks : BluetoothGattCallback
    {
        //public override void OnPhyRead(BluetoothGatt? gatt, [GeneratedEnum] ScanSettingsPhy txPhy, [GeneratedEnum] ScanSettingsPhy rxPhy, [GeneratedEnum] GattStatus status) => base.OnPhyRead(gatt, txPhy, rxPhy, status);
        //public override void OnPhyUpdate(BluetoothGatt? gatt, [GeneratedEnum] ScanSettingsPhy txPhy, [GeneratedEnum] ScanSettingsPhy rxPhy, [GeneratedEnum] GattStatus status) => base.OnPhyUpdate(gatt, txPhy, rxPhy, status);

        public Subject<GattCharacteristicEventArgs> CharacteristicRead { get; } = new Subject<GattCharacteristicEventArgs>();
        public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
            => this.CharacteristicRead.OnNext(new GattCharacteristicEventArgs(gatt, characteristic, status));


        public Subject<GattCharacteristicEventArgs> CharacteristicWrite { get; } = new Subject<GattCharacteristicEventArgs>();
        public override void OnCharacteristicWrite(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
            => this.CharacteristicWrite.OnNext(new GattCharacteristicEventArgs(gatt, characteristic, status));


        public Subject<GattCharacteristicEventArgs> CharacteristicChanged { get; } = new Subject<GattCharacteristicEventArgs>();
        public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
            => this.CharacteristicChanged.OnNext(new GattCharacteristicEventArgs(gatt, characteristic));


        public Subject<GattDescriptorEventArgs> DescriptorRead { get; } = new Subject<GattDescriptorEventArgs>();
        public override void OnDescriptorRead(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
            => this.DescriptorRead.OnNext(new GattDescriptorEventArgs(gatt, descriptor, status));


        public Subject<GattDescriptorEventArgs> DescriptorWrite { get; } = new Subject<GattDescriptorEventArgs>();
        public override void OnDescriptorWrite(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
            => this.DescriptorWrite.OnNext(new GattDescriptorEventArgs(gatt, descriptor, status));


        public Subject<MtuChangedEventArgs> MtuChanged { get; } = new Subject<MtuChangedEventArgs>();
        public override void OnMtuChanged(BluetoothGatt gatt, int mtu, GattStatus status)
            => this.MtuChanged.OnNext(new MtuChangedEventArgs(mtu, gatt, status));


        public Subject<GattRssiEventArgs> ReadRemoteRssi  { get; } = new Subject<GattRssiEventArgs>();
        public override void OnReadRemoteRssi(BluetoothGatt gatt, int rssi, GattStatus status)
            => this.ReadRemoteRssi.OnNext(new GattRssiEventArgs(rssi, gatt, status));


        public Subject<GattEventArgs> ReliableWriteCompleted { get; } = new Subject<GattEventArgs>();
        public override void OnReliableWriteCompleted(BluetoothGatt gatt, GattStatus status)
            => this.ReliableWriteCompleted.OnNext(new GattEventArgs(gatt, status));


        public Subject<GattEventArgs> ServicesDiscovered { get; } = new Subject<GattEventArgs>();
        public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
            => this.ServicesDiscovered.OnNext(new GattEventArgs(gatt, status));


        public Subject<ConnectionStateEventArgs> ConnectionStateChanged { get; } = new Subject<ConnectionStateEventArgs>();
        public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
            => this.ConnectionStateChanged.OnNext(new ConnectionStateEventArgs(gatt, status, newState));
    }
}
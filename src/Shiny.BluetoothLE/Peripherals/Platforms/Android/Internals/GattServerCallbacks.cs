using System;
using Android.Bluetooth;
using AGattStatus = Android.Bluetooth.GattStatus;


namespace Shiny.BluetoothLE.Peripherals.Internals
{
    public class GattServerCallbacks : BluetoothGattServerCallback
    {

        public event EventHandler<CharacteristicReadEventArgs> CharacteristicRead;
        public override void OnCharacteristicReadRequest(BluetoothDevice device,
                                                         int requestId,
                                                         int offset,
                                                         BluetoothGattCharacteristic characteristic)
        {
            this.CharacteristicRead?.Invoke(this, new CharacteristicReadEventArgs(device, characteristic, requestId, offset));
        }


        public event EventHandler<CharacteristicWriteEventArgs> CharacteristicWrite;
        public override void OnCharacteristicWriteRequest(BluetoothDevice device,
                                                          int requestId,
                                                          BluetoothGattCharacteristic characteristic,
                                                          bool preparedWrite,
                                                          bool responseNeeded,
                                                          int offset,
                                                          byte[] value)
        {
            this.CharacteristicWrite?.Invoke(this, new CharacteristicWriteEventArgs(characteristic, device, requestId, offset, preparedWrite, responseNeeded, value));
        }


        public event EventHandler<DescriptorReadEventArgs> DescriptorRead;
        public override void OnDescriptorReadRequest(BluetoothDevice device, int requestId, int offset, BluetoothGattDescriptor descriptor)
        {
            this.DescriptorRead?.Invoke(this, new DescriptorReadEventArgs(descriptor, device, requestId, offset));
        }


        public event EventHandler<DescriptorWriteEventArgs> DescriptorWrite;
        public override void OnDescriptorWriteRequest(BluetoothDevice device,
                                                      int requestId,
                                                      BluetoothGattDescriptor descriptor,
                                                      bool preparedWrite,
                                                      bool responseNeeded,
                                                      int offset,
                                                      byte[] value)
        {
            this.DescriptorWrite?.Invoke(this, new DescriptorWriteEventArgs(descriptor, device, requestId, offset, preparedWrite, responseNeeded, value));
        }


        public event EventHandler<ConnectionStateChangeEventArgs> ConnectionStateChanged;
        public override void OnConnectionStateChange(BluetoothDevice device, ProfileState status, ProfileState newState)
        {
            this.ConnectionStateChanged?.Invoke(this, new ConnectionStateChangeEventArgs(device, status, newState));
        }


        //public override void OnExecuteWrite(BluetoothDevice peripheral, int requestId, bool execute)
        //{
        //    base.OnExecuteWrite(peripheral, requestId, execute);
        //}


        //public event EventHandler<MtuChangedEventArgs> MtuChanged;
        //public override void OnMtuChanged(BluetoothDevice peripheral, int mtu)
        //{
        //    this.MtuChanged?.Invoke(this, new MtuChangedEventArgs(peripheral, mtu));
        //}


        //public event EventHandler<NotificationSentArgs> NotificationSent;
        //public override void OnNotificationSent(BluetoothDevice peripheral, AGattStatus status)
        //{
        //    this.NotificationSent?.Invoke(this, new NotificationSentArgs(peripheral, status));
        //}


        //public override void OnServiceAdded(ProfileState status, BluetoothGattService service)
        //{
        //    base.OnServiceAdded(status, service);
        //}
    }
}

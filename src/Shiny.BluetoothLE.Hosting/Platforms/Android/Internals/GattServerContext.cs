using System;
using System.Reactive.Subjects;
using Android.Bluetooth;
using AGattStatus = Android.Bluetooth.GattStatus;


namespace Shiny.BluetoothLE.Hosting.Internals
{
    public class GattServerContext : BluetoothGattServerCallback
    {
        public GattServerContext(IAndroidContext context)
        {
            this.Context = context;
            this.Manager = context.GetBluetooth();
        }


        public IAndroidContext Context { get; }
        public BluetoothManager Manager { get; }
        // subscribed device list


        BluetoothGattServer server;
        public BluetoothGattServer Server
        {
            get
            {
                this.server ??= this.Manager.OpenGattServer(this.Context.AppContext, this)!;
                return this.server!;
            }
        }


        public void CloseServer()
        {
            this.server.Close();
            this.server = null!;
        }


        public Subject<CharacteristicReadEventArgs> CharacteristicRead { get; } = new Subject<CharacteristicReadEventArgs>();
        public override void OnCharacteristicReadRequest(BluetoothDevice device,
                                                         int requestId,
                                                         int offset,
                                                         BluetoothGattCharacteristic characteristic)
            => this.CharacteristicRead.OnNext(new CharacteristicReadEventArgs(device, characteristic, requestId, offset));


        public Subject<CharacteristicWriteEventArgs> CharacteristicWrite { get; } = new Subject<CharacteristicWriteEventArgs>();
        public override void OnCharacteristicWriteRequest(BluetoothDevice device,
                                                          int requestId,
                                                          BluetoothGattCharacteristic characteristic,
                                                          bool preparedWrite,
                                                          bool responseNeeded,
                                                          int offset,
                                                          byte[] value)
            => this.CharacteristicWrite.OnNext(new CharacteristicWriteEventArgs(characteristic, device, requestId, offset, preparedWrite, responseNeeded, value));


        public Subject<DescriptorReadEventArgs> DescriptorRead { get; } = new Subject<DescriptorReadEventArgs>();
        public override void OnDescriptorReadRequest(BluetoothDevice device, int requestId, int offset, BluetoothGattDescriptor descriptor)
            => this.DescriptorRead.OnNext(new DescriptorReadEventArgs(descriptor, device, requestId, offset));


        public Subject<DescriptorWriteEventArgs> DescriptorWrite { get; } = new Subject<DescriptorWriteEventArgs>();
        public override void OnDescriptorWriteRequest(BluetoothDevice device,
                                                      int requestId,
                                                      BluetoothGattDescriptor descriptor,
                                                      bool preparedWrite,
                                                      bool responseNeeded,
                                                      int offset,
                                                      byte[] value)
            => this.DescriptorWrite.OnNext(new DescriptorWriteEventArgs(descriptor, device, requestId, offset, preparedWrite, responseNeeded, value));


        public Subject<ConnectionStateChangeEventArgs> ConnectionStateChanged { get; } = new Subject<ConnectionStateChangeEventArgs>();
        public override void OnConnectionStateChange(BluetoothDevice device, ProfileState status, ProfileState newState)
            => this.ConnectionStateChanged.OnNext(new ConnectionStateChangeEventArgs(device, status, newState));


        public Subject<GattEventArgs> NotificationSent { get; } = new Subject<GattEventArgs>();
        public override void OnNotificationSent(BluetoothDevice peripheral, AGattStatus status)
            => this.NotificationSent.OnNext(new GattEventArgs(peripheral));


        //public override void OnExecuteWrite(BluetoothDevice peripheral, int requestId, bool execute)
        //{
        //    base.OnExecuteWrite(peripheral, requestId, execute);
        //}


        //public event EventHandler<MtuChangedEventArgs> MtuChanged;
        //public override void OnMtuChanged(BluetoothDevice peripheral, int mtu)
        //{
        //    this.MtuChanged?.Invoke(this, new MtuChangedEventArgs(peripheral, mtu));
        //}


        //public override void OnServiceAdded(ProfileState status, BluetoothGattService service)
        //{
        //    base.OnServiceAdded(status, service);
        //}
    }
}

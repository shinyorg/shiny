using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;


namespace Shiny.BluetoothLE.Internals
{
    public class PeripheralContext
    {
        readonly object syncLock;
        readonly ManagerContext managerContext;
        readonly IList<GattCharacteristic> subscribers;
        readonly Subject<ConnectionState> connSubject;
        readonly ulong bluetoothAddress;


        public PeripheralContext(ManagerContext managerContext,
                                 IPeripheral peripheral,
                                 BluetoothLEDevice native)
        {
            this.syncLock = new object();
            this.connSubject = new Subject<ConnectionState>();
            this.managerContext = managerContext;
            this.subscribers = new List<GattCharacteristic>();
            this.Peripheral = peripheral;
            this.NativeDevice = native;
            this.bluetoothAddress = native.BluetoothAddress;
        }


        public IPeripheral Peripheral { get; }
        public BluetoothLEDevice? NativeDevice { get; private set; }
        public IObservable<ConnectionState> WhenStatusChanged() => this.connSubject.StartWith(this.Status);


        public async Task Connect()
        {
            if (this.NativeDevice != null && this.NativeDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
                return;

            this.connSubject.OnNext(ConnectionState.Connecting);
            this.NativeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(this.bluetoothAddress);
            this.NativeDevice.ConnectionStatusChanged += this.OnNativeConnectionStatusChanged;
            await this.NativeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached); // HACK: kick the connection on
        }


        public async Task Disconnect()
        {
            if (this.NativeDevice == null)
                return;

            this.connSubject.OnNext(ConnectionState.Disconnecting);
            foreach (var ch in this.subscribers)
            {
                try
                {
                    await ch.Disconnect();
                }
                catch (Exception e)
                {
                    //Log.Info(BleLogCategory.Device, "Disconnect Error - " + e);
                }
            }
            this.subscribers.Clear();

            this.managerContext.RemovePeripheral(this.NativeDevice.BluetoothAddress);
            this.NativeDevice.ConnectionStatusChanged -= this.OnNativeConnectionStatusChanged;
            this.NativeDevice?.Dispose();
            this.NativeDevice = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            this.connSubject.OnNext(ConnectionState.Disconnected);
        }


        public void SetNotifyCharacteristic(GattCharacteristic characteristic)
        {
            lock (this.syncLock)
            {
                if (characteristic.IsNotifying)
                {
                    this.subscribers.Add(characteristic);
                }
                else
                {
                    this.subscribers.Remove(characteristic);
                }
            }
        }


        public ConnectionState Status
        {
            get
            {
                if (this.NativeDevice == null)
                    return ConnectionState.Disconnected;

                switch (this.NativeDevice.ConnectionStatus)
                {
                    case BluetoothConnectionStatus.Connected:
                        return ConnectionState.Connected;

                    default:
                        return ConnectionState.Disconnected;
                }
            }
        }


        void OnNativeConnectionStatusChanged(BluetoothLEDevice sender, object args) =>
            this.connSubject.OnNext(this.Status);
    }
}
using System;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace Shiny.BluetoothLE;


public partial class Peripheral : IPeripheral
{
    public Peripheral(DeviceInformation deviceInfo, BluetoothLEDevice device)
    {
        this.Native = device;
        this.DeviceInfo = deviceInfo;

        this.Uuid = device.BluetoothDeviceId!.ToString();
    }



    public BluetoothLEDevice? Native { get; private set; }
    public DeviceInformation DeviceInfo { get; }

    public string Uuid { get; }
    public string? Name =>this.Native?.Name;
    public int Mtu => -1;


    public ConnectionState Status
    {
        get
        {
            if (this.Native == null)
                return ConnectionState.Disconnected;

            return this.Native.ConnectionStatus switch
            {
                BluetoothConnectionStatus.Connected => ConnectionState.Connected,
                _ => ConnectionState.Disconnected
            };
        }
    }


    public IObservable<BleException> WhenConnectionFailed() => null;

    public void Connect(ConnectionConfig? config)
    {
        //if (this.NativeDevice != null && this.NativeDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
        //    return;

        //this.connSubject.OnNext(ConnectionState.Connecting);
        //this.NativeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(this.bluetoothAddress);
        //this.NativeDevice.ConnectionStatusChanged += this.OnNativeConnectionStatusChanged;
        //await this.NativeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached); // HACK: kick the connection on
    }


    public void CancelConnection()
    {
        if (this.Native == null)
            return;

        foreach (var service in this.Native.GattServices)
        {
            service.Session?.Dispose();
            service.Dispose();
        }
        this.Native.Dispose();
        this.Native = null;

        GC.Collect();        
    }    


    const string SS_KEY = "System.Devices.Aep.SignalStrength";
    public IObservable<int> ReadRssi() => Observable.Create<int>(ob =>
    {
        //if (this.DeviceInfo.Properties.ContainsKey(SS_KEY))
        return () => { };
    });


    public IObservable<ConnectionState> WhenStatusChanged() => throw new NotImplementedException();
}

/*

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
 */
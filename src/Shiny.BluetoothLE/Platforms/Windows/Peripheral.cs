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

        this.Uuid = device.BluetoothDeviceId.ToString();
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

            this.Uuid = native.GetDeviceId().ToString();


        public override void Connect(ConnectionConfig? config) => this.context.Connect();
        public override IObservable<IGattService?> GetKnownService(string serviceUuid, bool throwIfNotFound = false) => Observable
            .FromAsync(async ct =>
            {
                var uuid = Utils.ToUuidType(serviceUuid);
                var result = await this.context.NativeDevice.GetGattServicesForUuidAsync(uuid, BluetoothCacheMode.Cached);
                if (result.Status != GattCommunicationStatus.Success)
                    throw new ArgumentException("Could not find GATT service - " + result.Status);

                var wrap = new GattService(this.context, result.Services.First());
                return wrap;
            })
            .Assert(serviceUuid, throwIfNotFound);


        public override IObservable<IList<IGattService>> GetServices() => Observable.FromAsync(async ct =>
        {
           
        });


        IObservable<string> nameOb;
        public override IObservable<string> WhenNameUpdated()
        {
            this.nameOb = this.nameOb ?? Observable.Create<string>(ob =>
            {
                var handler = new TypedEventHandler<BluetoothLEDevice, object>(
                    (sender, args) => ob.OnNext(this.Name)
                );
                var sub = this.WhenConnected().Subscribe(_ =>
                    this.context.NativeDevice.NameChanged += handler
                );
                return () =>
                {
                    sub?.Dispose();
                    if (this.context.NativeDevice != null)
                        this.context.NativeDevice.NameChanged -= handler;
                };
            })
            .StartWith(this.Name)
            .Publish()
            .RefCount();

            return this.nameOb;
        }


        public IGattReliableWriteTransaction BeginReliableWriteTransaction() => new GattReliableWriteTransaction();


    }
}






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


        void OnNativeConnectionStatusChanged(BluetoothLEDevice sender, object args) =>
            this.connSubject.OnNext(this.Status);
    }
}
 */
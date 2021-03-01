using System;
using System.Collections.Concurrent;
using System.Linq;
using Windows.Devices.Bluetooth;


namespace Shiny.BluetoothLE.Internals
{
    public class ManagerContext
    {
        readonly ConcurrentDictionary<ulong, IPeripheral> peripherals = new ConcurrentDictionary<ulong, IPeripheral>();


        public IPeripheral AddPeripheral(BluetoothLEDevice native)
        {
            var dev = new Peripheral(this, native);
            this.peripherals.TryAdd(native.BluetoothAddress, dev);
            return dev;
        }


        public IPeripheral GetPeripheral(ulong bluetoothAddress)
        {
            this.peripherals.TryGetValue(bluetoothAddress, out var peripheral);
            return peripheral;
        }


        public IPeripheral AddOrGetPeripheral(BluetoothLEDevice native)
        {
            var peripheral = this.peripherals.GetOrAdd(native.BluetoothAddress, id => new Peripheral(this, native));
            return peripheral;
        }


        public bool RemovePeripheral(ulong bluetoothAddress)
            => this.peripherals.TryRemove(bluetoothAddress, out _);


        //public IEnumerable<IPeripheral> GetConnectedPeripherals() => this.peripherals
        //    .Where(x => x.Value.Status == ConnectionStatus.Connected)
        //    .Select(x => x.Value)
        //    .ToList();


        public void Clear() => this.peripherals
            .Where(x => x.Value.Status != ConnectionState.Connected)
            .ToList()
            .ForEach(x => this.peripherals.TryRemove(x.Key, out _));
    }
}
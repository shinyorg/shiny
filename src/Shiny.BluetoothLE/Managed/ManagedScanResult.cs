using System;


namespace Shiny.BluetoothLE.Managed
{
    public class ManagedScanResult : NotifyPropertyChanged
    {
        public ManagedScanResult(IPeripheral peripheral, string[]? serviceUuids)
        {
            this.Peripheral = peripheral;
            this.ServiceUuids = serviceUuids;
        }


        public IPeripheral Peripheral { get; }
        public string[]? ServiceUuids { get; }
        public bool IsConnected => this.Peripheral.IsConnected();
        public string Uuid => this.Peripheral.Uuid;


        bool? connectable;
        public bool? Connectable
        {
            get => this.connectable;
            internal set => this.Set(ref this.connectable, value);
        }


        string name;
        public string Name
        {
            get => this.name;
            internal set => this.Set(ref this.name, value);
        }


        int rssi;
        public int Rssi
        {
            get => this.rssi;
            internal set => this.Set(ref this.rssi, value);
        }


        ManufacturerData? manufacturerData;
        public ManufacturerData? ManufacturerData
        {
            get => this.manufacturerData;
            internal set => this.Set(ref this.manufacturerData, value);
        }


        DateTimeOffset lastSeen;
        public DateTimeOffset LastSeen
        {
            get => this.lastSeen;
            internal set => this.Set(ref this.lastSeen, value);
        }
    }
}

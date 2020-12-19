using System;
using System.Diagnostics;
using Shiny.BluetoothLE;
using ReactiveUI.Fody.Helpers;


namespace Samples.BluetoothLE
{
    public class PeripheralItemViewModel : ViewModel
    {
        public PeripheralItemViewModel(IPeripheral peripheral)
            => this.Peripheral = peripheral;


        public override bool Equals(object obj)
            => this.Peripheral.Equals(obj);

        public IPeripheral Peripheral { get; }
        public string Uuid => this.Peripheral.Uuid;

        [Reactive] public string Name { get; private set; }
        [Reactive] public bool IsConnected { get; private set; }
        [Reactive] public int Rssi { get; private set; }
        [Reactive] public string Connectable { get; private set; }
        [Reactive] public int ServiceCount { get; private set; }
        //[Reactive] public string ManufacturerData { get; private set; }
        [Reactive] public string LocalName { get; private set; }
        [Reactive] public int TxPower { get; private set; }


        public void Update(ScanResult result)
        {
            using (this.SuppressChangeNotifications())
            {
                this.Name = this.Peripheral.Name;
                this.Rssi = result.Rssi;

                var ad = result.AdvertisementData;
                this.ServiceCount = ad.ServiceUuids?.Length ?? 0;
                this.Connectable = ad?.IsConnectable?.ToString() ?? "Unknown";
                this.LocalName = ad.LocalName;
                this.TxPower = ad.TxPower;
                //this.ManufacturerData = ad.ManufacturerData == null
                //    ? null
                //    : BitConverter.ToString(ad.ManufacturerData);
            }
        }
    }
}
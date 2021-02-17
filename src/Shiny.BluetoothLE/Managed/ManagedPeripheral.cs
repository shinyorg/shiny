using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;

namespace Shiny.BluetoothLE.Managed
{
    public class ManagedCharacteristic
    {
        public string ServiceUuid { get; set; }
        public string CharacteristicUuid { get; set; }
        public bool UseIndicationIfAvailable { get; set; }

        public byte[] Value { get; set; }
    }


    // TODO: auto connect based on read/write/notification
    // TODO: watch encryption
    // TODO: pairing
    public class ManagedPeripheral : NotifyPropertyChanged, IDisposable
    {
        IDisposable? rssiSub;

        public ManagedPeripheral(IPeripheral peripheral)
        {
            this.Peripheral = peripheral;

            this.Peripheral
                .WhenConnected()
                .Subscribe(_ =>
                {
                    // restore notifications
                });
        }


        public IPeripheral Peripheral { get; }


        // bool observable
        public string Name { get; set; }
        public ConnectionState Status => this.Peripheral.Status;
        public int Rssi { get; set; }


        public bool IsMonitoringRssi => this.rssiSub != null;

        //public IObservable<(string ServiceUuid, string CharacteristicUuid)> WhenNotificationReady() => null;
        public IObservable<GattCharacteristicResult> WhenNotificationReceived() => null;

        public IReadOnlyList<ManagedCharacteristic> Characteristics { get; set; }

        public IObservable<Unit> Write(ManagedCharacteristic characteristic) => null;
        public IObservable<Unit> Write(string serviceUuid, string characteristicUuid) => null; // update value on managed chars?
        public IObservable<Unit> Read(ManagedCharacteristic characteristic) => null;
        public IObservable<byte[]> Read(string serviceUuid, string characteristicUuid) => null; // update value on managed chars
        public IObservable<Unit> EnableNotification(bool enable, string serviceUuid, string characteristicUuid, bool useIndicationIfAvailable) => null;


        // TODO: should connection be managed?

        public void ToggleRssiUpdates()
        {
            if (this.rssiSub == null)
            {
                this.rssiSub = this.Peripheral
                    .ReadRssiContinuously()
                    .Subscribe(x => this.Rssi = x);
            }
            this.rssiSub?.Dispose();
            this.rssiSub = null;
        }

        public void Dispose()
        {
            // dispose of INPC
            //this.ToggleRssiUpdates();
            this.Peripheral.CancelConnection();
        }
    }
}

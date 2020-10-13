using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Shiny.BluetoothLE;


namespace Shiny.Testing.BluetoothLE
{
    public class TestBleManager : IBleManager
    {
        readonly Subject<AccessState> statSubj = new Subject<AccessState>();


        AccessState accessState = AccessState.Available;
        public AccessState Status
        {
            get => this.accessState;
            set
            {
                this.accessState = value;
                this.statSubj.OnNext(value);
            }
        }


        public bool IsScanning { get; private set; }
        public IObservable<AccessState> RequestAccess() => Observable.Return(this.Status);
        public IObservable<AccessState> WhenStatusChanged() => this.statSubj.StartWith(this.Status);


        public List<IPeripheral> Peripherals { get; set; } = new List<IPeripheral>();


        public IObservable<IPeripheral?> GetKnownPeripheral(string peripheralId)
        {
            var peripheral = this.Peripherals.FirstOrDefault(x => x.Uuid == peripheralId);
            return Observable.Return(peripheral);
        }


        public IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(string? serviceUuid = null)
        {
            var peripherals = this.Peripherals.Where(x => x.IsConnected());
            return Observable.Return(peripherals);
        }


        public void ScanResult(string peripheralUuid, int rssi, IAdvertisementData adData)
        {
            var peripheral = this.Peripherals.FirstOrDefault(x => x.Uuid == peripheralUuid);
            if (peripheral == null)
                throw new ArgumentException("No test peripheral found in Peripheral collection with UUID " + peripheralUuid);

            var result = new ScanResult(peripheral, rssi, adData);
            this.scanSubj.OnNext(result);
        }


        readonly Subject<ScanResult> scanSubj = new Subject<ScanResult>();
        public IObservable<ScanResult> Scan(ScanConfig? config = null) => Observable.Create<ScanResult>(ob =>
        {
            this.IsScanning = true;
            return this.scanSubj
                .Finally(() => this.IsScanning = false)
                .Subscribe(ob.OnNext);
        });


        public void StopScan()
        {
        }
    }
}

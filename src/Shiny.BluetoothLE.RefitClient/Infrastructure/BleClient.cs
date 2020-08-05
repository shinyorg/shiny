using System;
using System.Reactive;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE.RefitClient.Infrastructure
{
    public class BleClient : IBleClient
    {
        public IPeripheral Peripheral { get; internal set; }
        public IBleDataSerializer Serializer { get; set; }

        public IObservable<Unit> Connect() => this.Peripheral.ConnectWait().Select(_ => Unit.Default);
        public void Disconnect() => this.Peripheral.CancelConnection();
    }
}

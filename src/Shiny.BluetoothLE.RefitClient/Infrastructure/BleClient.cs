using System;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE.RefitClient.Infrastructure
{
    public class BleClient : IBleClient
    {
        IPeripheral peripheral;
        public IPeripheral Peripheral
        {
            get
            {
                if (this.peripheral == null)
                    throw new ArgumentException("Peripheral is not set");

                return this.peripheral;
            }
            internal set => this.peripheral = value;
        }


        public IBleDataSerializer Serializer { get; set; }


        protected IObservable<IGattCharacteristic> Char(string serviceUuid, string characteristicUuid) => this.Peripheral
            .ConnectWait()
            .Select(x => x.GetKnownCharacteristics(
                Guid.Parse(serviceUuid),
                Guid.Parse(characteristicUuid)
            ))
            .Timeout(TimeSpan.FromSeconds(20))
            .Switch();
    }
}

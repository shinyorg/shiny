using System;


namespace Acr.BluetoothLE.Central
{
    public class GattService : IGattService
    {
        public IPeripheral Peripheral { get; }
        public Guid Uuid { get; }
        public string Description { get; }
        public IObservable<IGattCharacteristic> DiscoverCharacteristics()
        {
            throw new NotImplementedException();
        }

        public IObservable<IGattCharacteristic> GetKnownCharacteristics(params Guid[] characteristicIds)
        {
            throw new NotImplementedException();
        }
    }
}

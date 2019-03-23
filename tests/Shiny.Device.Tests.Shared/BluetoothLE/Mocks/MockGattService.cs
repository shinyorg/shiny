using System;
using Shiny.BluetoothLE.Central;


namespace Shiny.Devices.Tests.BluetoothLE.Mocks
{
    public class MockGattService : IGattService
    {

        public IPeripheral Peripheral { get; set; }

        public Guid Uuid => this.Peripheral?.Uuid ?? Guid.NewGuid();

        public string Description => throw new NotImplementedException();

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

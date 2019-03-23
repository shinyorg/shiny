using System;


namespace Acr.BluetoothLE.Central
{
    public class GattDescriptor : IGattDescriptor
    {
        public IGattCharacteristic Characteristic { get; }
        public Guid Uuid { get; }
        public string Description { get; }
        public byte[] Value { get; }
        public IObservable<DescriptorGattResult> Write(byte[] data)
        {
            throw new NotImplementedException();
        }

        public IObservable<DescriptorGattResult> Read()
        {
            throw new NotImplementedException();
        }
    }
}

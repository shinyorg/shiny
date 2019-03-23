using System;


namespace Acr.BluetoothLE.Central
{
    public class GattCharacteristic : IGattCharacteristic
    {
        public IGattService Service { get; }
        public Guid Uuid { get; }
        public string Description { get; }
        public bool IsNotifying { get; }
        public CharacteristicProperties Properties { get; }
        public byte[] Value { get; }
        public IObservable<CharacteristicGattResult> EnableNotifications(bool useIndicationIfAvailable = false)
        {
            throw new NotImplementedException();
        }

        public IObservable<CharacteristicGattResult> DisableNotifications()
        {
            throw new NotImplementedException();
        }

        public IObservable<CharacteristicGattResult> WhenNotificationReceived()
        {
            throw new NotImplementedException();
        }

        public IObservable<IGattDescriptor> DiscoverDescriptors()
        {
            throw new NotImplementedException();
        }

        public IObservable<CharacteristicGattResult> WriteWithoutResponse(byte[] value)
        {
            throw new NotImplementedException();
        }

        public IObservable<CharacteristicGattResult> Write(byte[] value)
        {
            throw new NotImplementedException();
        }

        public IObservable<CharacteristicGattResult> Read()
        {
            throw new NotImplementedException();
        }
    }
}

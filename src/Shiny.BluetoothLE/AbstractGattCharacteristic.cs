using System;


namespace Shiny.BluetoothLE
{
    public abstract class AbstractGattCharacteristic : IGattCharacteristic
    {
        protected AbstractGattCharacteristic(IGattService service, Guid uuid, CharacteristicProperties properties)
        {
            this.Service = service;
            this.Uuid = uuid;
            this.Properties = properties;
        }


        public IGattService Service { get; }
        public bool IsNotifying { get; protected set; }
        public Guid Uuid { get; }
        public CharacteristicProperties Properties { get; }

        public abstract IObservable<IGattDescriptor> DiscoverDescriptors();
        public abstract IObservable<CharacteristicGattResult> Notify(bool sendHookEvent, bool enableIndicationsIfAvailable);
        public abstract IObservable<CharacteristicGattResult> Read();
        public abstract IObservable<CharacteristicGattResult> Write(byte[] value, bool withResponse = true);
    }
}

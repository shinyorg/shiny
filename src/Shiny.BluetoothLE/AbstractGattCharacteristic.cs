using System;
using System.Reactive;

namespace Shiny.BluetoothLE
{
    public abstract class AbstractGattCharacteristic : IGattCharacteristic
    {
        protected AbstractGattCharacteristic(IGattService service, string uuid, CharacteristicProperties properties)
        {
            this.Service = service;
            this.Uuid = uuid;
            this.Properties = properties;
        }


        public IGattService Service { get; }
        public bool IsNotifying { get; protected set; }
        public string Uuid { get; }
        public CharacteristicProperties Properties { get; }

        public abstract IObservable<IGattDescriptor> DiscoverDescriptors();
        public abstract IObservable<Unit> EnableNotifications(bool enable, bool useIndicationIfAvailable = false);
        public abstract IObservable<CharacteristicGattResult> Notify(bool autoEnabled = true, bool enableIndicationsIfAvailable = false);
        public abstract IObservable<CharacteristicGattResult> Read();
        public abstract IObservable<CharacteristicGattResult> Write(byte[] value, bool withResponse = true);
    }
}

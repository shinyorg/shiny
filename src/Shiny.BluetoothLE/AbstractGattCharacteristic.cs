using System;
using System.Collections.Generic;
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

        public abstract IObservable<IList<IGattDescriptor>> GetDescriptors();
        public abstract IObservable<IGattCharacteristic> EnableNotifications(bool enable, bool useIndicationIfAvailable = false);
        public abstract IObservable<GattCharacteristicResult> WhenNotificationReceived();
        public abstract IObservable<GattCharacteristicResult> Read();
        public abstract IObservable<GattCharacteristicResult> Write(byte[] value, bool withResponse = true);
    }
}

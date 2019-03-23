using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;


namespace Shiny.BluetoothLE.Central
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
        public virtual string Description => Dictionaries.GetCharacteristicDescription(this.Uuid);
        public bool IsNotifying { get; protected set; }
        public Guid Uuid { get; }
        public CharacteristicProperties Properties { get; }
        public abstract byte[] Value { get; }

        public abstract IObservable<IGattDescriptor> DiscoverDescriptors();
        public abstract IObservable<CharacteristicGattResult> EnableNotifications(bool enableIndicationsIfAvailable);
        public abstract IObservable<CharacteristicGattResult> DisableNotifications();
        public abstract IObservable<CharacteristicGattResult> Read();
        public abstract IObservable<CharacteristicGattResult> WriteWithoutResponse(byte[] value);
        public abstract IObservable<CharacteristicGattResult> Write(byte[] value);
        public abstract IObservable<CharacteristicGattResult> WhenNotificationReceived();
    }
}

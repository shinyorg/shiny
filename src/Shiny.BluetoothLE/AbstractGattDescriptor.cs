using System;


namespace Shiny.BluetoothLE
{
    public abstract class AbstractGattDescriptor : IGattDescriptor
    {
        protected AbstractGattDescriptor(IGattCharacteristic characteristic, Guid uuid)
        {
            this.Characteristic = characteristic;
            this.Uuid = uuid;
        }


        public IGattCharacteristic Characteristic { get; }
        public virtual string Description => Dictionaries.GetDescriptorDescription(this.Uuid);

        public abstract byte[] Value { get; }
        public Guid Uuid { get; }
        public abstract IObservable<DescriptorGattResult> Write(byte[] data);
        public abstract IObservable<DescriptorGattResult> Read();
    }
}
using System;


namespace Shiny.BluetoothLE
{
    public abstract class AbstractGattDescriptor : IGattDescriptor
    {
        protected AbstractGattDescriptor(IGattCharacteristic characteristic, string uuid)
        {
            this.Characteristic = characteristic;
            this.Uuid = uuid;
        }


        public IGattCharacteristic Characteristic { get; }
        public string Uuid { get; }
        public abstract IObservable<DescriptorGattResult> Write(byte[] data);
        public abstract IObservable<DescriptorGattResult> Read();
    }
}
using System;


namespace Shiny.BluetoothLE
{
    public interface IGattDescriptor
    {
        IGattCharacteristic Characteristic { get; }
        Guid Uuid { get; }
        IObservable<DescriptorGattResult> Write(byte[] data);
        IObservable<DescriptorGattResult> Read();
    }
}

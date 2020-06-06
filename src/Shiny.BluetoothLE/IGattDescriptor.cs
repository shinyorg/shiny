using System;


namespace Shiny.BluetoothLE
{
    public interface IGattDescriptor
    {
        IGattCharacteristic Characteristic { get; }
        Guid Uuid { get; }
        string Description { get; }
        byte[] Value { get; }

        IObservable<DescriptorGattResult> Write(byte[] data);
        IObservable<DescriptorGattResult> Read();
    }
}

using System;


namespace Shiny.BluetoothLE
{
    public interface IGattDescriptor
    {
        IGattCharacteristic Characteristic { get; }
        string Uuid { get; }
        IObservable<DescriptorGattResult> Write(byte[] data);
        IObservable<DescriptorGattResult> Read();
    }
}

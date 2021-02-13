using System;


namespace Shiny.BluetoothLE
{
    public interface IGattDescriptor
    {
        IGattCharacteristic Characteristic { get; }
        string Uuid { get; }
        IObservable<GattDescriptorResult> Write(byte[] data);
        IObservable<GattDescriptorResult> Read();
    }
}

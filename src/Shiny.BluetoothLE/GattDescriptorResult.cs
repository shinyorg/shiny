using System;


namespace Shiny.BluetoothLE
{
    public class GattDescriptorResult
    {
        public GattDescriptorResult(IGattDescriptor descriptor, byte[]? data)
        {
            this.Descriptor = descriptor;
            this.Data = data;
        }


        public byte[]? Data { get; }
        public IGattDescriptor Descriptor { get; }
    }
}

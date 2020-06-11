using System;


namespace Shiny.BluetoothLE
{
    public class DescriptorGattResult
    {
        public DescriptorGattResult(IGattDescriptor descriptor, byte[]? data)
        {
            this.Descriptor = descriptor;
            this.Data = data;
        }


        public byte[]? Data { get; }
        public IGattDescriptor Descriptor { get; }
    }
}

using System;
using Android.Bluetooth;


namespace Shiny.BluetoothLE.Hosting.Internals
{
    public class DescriptorWriteEventArgs : WriteRequestEventArgs
    {
        public DescriptorWriteEventArgs(
            BluetoothGattDescriptor descriptor,
            BluetoothDevice device,
            int requestId,
            int offset,
            bool preparedWrite,
            bool responseNeeded,
            byte[] value) : base(device, requestId, offset, preparedWrite, responseNeeded, value)
        {
            this.Descriptor = descriptor;
        }


        public BluetoothGattDescriptor Descriptor { get; }
    }
}
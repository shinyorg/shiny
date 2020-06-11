using System;
using Android.Bluetooth;


namespace Shiny.BluetoothLE.Hosting.Internals
{
    public abstract class WriteRequestEventArgs : GattRequestEventArgs
    {
        protected WriteRequestEventArgs(
            BluetoothDevice device,
            int requestId,
            int offset,
            bool preparedWrite,
            bool responseNeeded,
            byte[] value) : base(device, requestId, offset)
        {
            this.PreparedWrite = preparedWrite;
            this.ResponseNeeded = responseNeeded;
            this.Value = value;
        }


        public bool ResponseNeeded { get; }
        public bool PreparedWrite { get; }
        public byte[] Value { get; }
    }
}
using System;
using Android.Bluetooth;


namespace Shiny.BluetoothLE.Peripherals.Internals
{
    public class GattServerContext
    {
        public GattServerContext(AndroidContext context)
        {
            this.Context = context;
            this.Manager = context.GetBluetooth();
            this.Callbacks = new GattServerCallbacks();
        }

        public AndroidContext Context { get; }
        public BluetoothManager Manager { get; }
        public GattServerCallbacks Callbacks { get; }
        // subscribed device list


        BluetoothGattServer server;
        public BluetoothGattServer Server
        {
            get
            {
                if (this.server == null)
                {
                    this.server = this.Manager.OpenGattServer(this.Context.AppContext, this.Callbacks);
                }
                return this.server;
            }
        }


        public void CloseServer()
        {
            this.server.Close();
            this.server = null;
        }
    }
}

using System;
using System.Threading.Tasks;
using Shiny;
using Shiny.BluetoothLE;


namespace Sample
{
    public class BleClientDelegate : BleDelegate
    {
        readonly SampleSqliteConnection conn;


        public BleClientDelegate(SampleSqliteConnection conn)
        {
            this.conn = conn;
        }


        public override Task OnAdapterStateChanged(AccessState state)
        {
            return this.conn.InsertAsync(new ShinyEvent
            {
                Text = "BLE Adapter Status",
                Detail = $"New Status: {state}",
                Timestamp = DateTime.Now
            });
        }


        public override Task OnConnected(IPeripheral peripheral)
        {
            return this.conn.InsertAsync(new ShinyEvent
            {
                Text = "Peripheral Connected",
                Detail = peripheral.Name,
                Timestamp = DateTime.Now
            });
        }
    }
}
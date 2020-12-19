using System;
using System.Threading.Tasks;
using Samples.Infrastructure;
using Samples.Models;
using Shiny;
using Shiny.BluetoothLE;


namespace Samples.BluetoothLE
{
    public class BleClientDelegate : BleDelegate, IShinyStartupTask
    {
        readonly CoreDelegateServices services;
        public BleClientDelegate(CoreDelegateServices services) => this.services = services;


        public override async Task OnAdapterStateChanged(AccessState state)
        {
            if (state == AccessState.Disabled)
                await this.services.Notifications.Send(this.GetType(), true, "BLE State", "Turn on Bluetooth already");
        }


        public override Task OnConnected(IPeripheral peripheral) => Task.WhenAll(
            this.services.Connection.InsertAsync(new BleEvent
            {
                Timestamp = DateTime.Now
            }),
            this.services.Notifications.Send(
                this.GetType(),
                true,
                "BluetoothLE Device Connected",
                $"{peripheral.Name} has connected"
            )
        );


        //public override Task OnScanResult(ScanResult result)
        //{
        //    // we only want this to run in the background
        //    return base.OnScanResult(result);
        //}


        public void Start()
            => this.services.Notifications.Register(this.GetType(), false, "BluetoothLE");
    }
}

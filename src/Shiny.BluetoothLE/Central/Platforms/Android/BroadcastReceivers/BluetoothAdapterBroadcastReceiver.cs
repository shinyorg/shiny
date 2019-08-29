using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;


namespace Shiny.BluetoothLE.Central
{
    [BroadcastReceiver(
        Name = "com.shiny.bluetoothle.BluetoothAdapterBroadcastReceiver",
        Enabled = true,
        Exported = true
    )]
    [IntentFilter(new [] {
        BluetoothAdapter.ActionConnectionStateChanged
    })]
    public class BluetoothAdapterBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent) => this.Execute(async () =>
        {
        });
    }
}
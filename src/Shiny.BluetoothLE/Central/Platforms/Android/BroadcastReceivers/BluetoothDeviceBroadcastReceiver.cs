using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;


namespace Shiny.BluetoothLE.Central.BroadcastReceivers
{
    [BroadcastReceiver(
        Name = "com.shiny.bluetoothle.BluetoothDeviceBroadcastReceiver",
        Enabled = true,
        Exported = true
    )]
    [IntentFilter(new[] {
        BluetoothDevice.ActionAclConnected,
        BluetoothDevice.ActionAclDisconnected,
        BluetoothDevice.ActionBondStateChanged,
        BluetoothDevice.ActionNameChanged,
        BluetoothDevice.ActionPairingRequest
    })]
    public class BluetoothDeviceBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent) => this.Execute(async () =>
        {
            //BluetoothDevice.ExtraDevice
        });
    }
}

using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;


namespace Shiny.BluetoothLE.Internals
{
    [BroadcastReceiver(
        Name = BleManager.BroadcastReceiverName,
        Enabled = true,
        Exported = true
    )]
    [IntentFilter(new[] {
        BluetoothDevice.ActionNameChanged,
        BluetoothDevice.ActionBondStateChanged,
        BluetoothDevice.ActionPairingRequest,
        BluetoothDevice.ActionAclConnected
    })]
    public class ShinyBleBroadcastReceiver : BroadcastReceiver
    {
        internal static Action<Intent?>? OnBleEvent { get; set; }
        public override void OnReceive(Context? context, Intent? intent)
            => OnBleEvent?.Invoke(intent);
    }
}

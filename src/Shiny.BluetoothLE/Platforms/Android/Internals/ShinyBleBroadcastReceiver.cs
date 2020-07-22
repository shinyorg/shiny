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
        BluetoothDevice.ActionPairingRequest
    })]
    public class ShinyBleBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            try
            {
                ShinyHost.Resolve<CentralContext>().DeviceEvent(intent);
            }
            catch (Exception ex)
            {
                Logging.Log.Write(ex);
            }
        }
    }
}

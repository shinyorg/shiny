using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Microsoft.Extensions.Logging;


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
        // TODO: may need to be async if broadcast receiver has a quick stop
        public override void OnReceive(Context context, Intent intent)
        {
            try
            {
                ShinyHost.Resolve<ManagerContext>().DeviceEvent(intent);
            }
            catch (Exception ex)
            {
                ShinyHost
                    .LoggerFactory
                    .CreateLogger<ILogger<ShinyBleBroadcastReceiver>>()
                    .LogError(ex, "Error executing in broadcast receiver");
            }
        }
    }
}

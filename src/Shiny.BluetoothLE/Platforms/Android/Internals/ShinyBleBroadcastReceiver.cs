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
        BluetoothDevice.ActionAclConnected,
        BluetoothDevice.ActionAclDisconnected,
        BluetoothDevice.ActionBondStateChanged,
        BluetoothDevice.ActionNameChanged,
        BluetoothDevice.ActionPairingRequest
    })]
    public class ShinyBleBroadcastReceiver : BroadcastReceiver
    {
        readonly Lazy<CentralContext> context = ShinyHost.LazyResolve<CentralContext>();


        public override void OnReceive(Context context, Intent intent) => this.Execute(async () =>
        {
            var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
            this.context.Value.DeviceEvent(intent.Action, device);
        });
    }
}

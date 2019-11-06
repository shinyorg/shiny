using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;


namespace Shiny.BluetoothLE.Central.Internals
{
    [BroadcastReceiver(
        Name = CentralManager.BroadcastReceiverName,
        Enabled = true,
        Exported = true
    )]
    [IntentFilter(new[] {
        BluetoothAdapter.ActionStateChanged,
        BluetoothDevice.ActionAclConnected,
        BluetoothDevice.ActionAclDisconnected,
        BluetoothDevice.ActionBondStateChanged,
        BluetoothDevice.ActionNameChanged,
        BluetoothDevice.ActionPairingRequest
    })]
    public class ShinyBleCentralBroadcastReceiver : BroadcastReceiver
    {
        readonly CentralContext context;


        public ShinyBleCentralBroadcastReceiver()
        {
            this.context = ShinyHost.Resolve<CentralContext>();
        }


        public override void OnReceive(Context context, Intent intent) => this.Execute(async () =>
        {
            if (intent.Action.Equals(BluetoothAdapter.ActionStateChanged))
            {
                var nativeState = (State)Enum.Parse(
                    typeof(State),
                    intent.GetStringExtra(BluetoothAdapter.ExtraState)
                );
                this.context.ChangeStatus(nativeState);
            }
            else
            {
                var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                this.context.DeviceEvent(intent.Action, device);
            }
        });
    }
}

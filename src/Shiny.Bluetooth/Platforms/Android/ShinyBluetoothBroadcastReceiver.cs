using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;


namespace Shiny.Bluetooth
{
    [BroadcastReceiver(
        Name = "com.shiny.bluetoothle.ShinyBluetoothBroadcastReceiver",
        Enabled = true,
        Exported = true
    )]
    [IntentFilter(new[] {
        BluetoothAdapter.ActionStateChanged,
        BluetoothAdapter.ActionDiscoveryStarted,
        BluetoothAdapter.ActionDiscoveryFinished,
        BluetoothDevice.ActionAclConnected,
        BluetoothDevice.ActionAclDisconnected,
        BluetoothDevice.ActionAclDisconnectRequested,
        //BluetoothDevice.ActionPairingRequest
    })]
    public class ShinyBleAdapterStateBroadcastReceiver : ShinyBroadcastReceiver
    {
        readonly Lazy<IMessageBus> messageBus = new Lazy<IMessageBus>(() => ShinyHost.Resolve<IMessageBus>());
        //android.intent.action.AIRPLANE_MOD

        public override void OnReceive(Context context, Intent intent)
        {
            switch (intent.Action)
            {
                case BluetoothAdapter.ActionConnectionStateChanged:
                    var newState = intent.GetIntExtra(BluetoothAdapter.ExtraState, -1);
                    this.messageBus.Value.Publish(newState);
                    break;
            }
        }
    }
}

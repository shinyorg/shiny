using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;


namespace Shiny.BluetoothLE
{
    [BroadcastReceiver(
        Name = "com.shiny.bluetoothle.ShinyBleAdapterStateBroadcastReceiver",
        Enabled = true,
        Exported = true
    )]
    [IntentFilter(new[] {
        BluetoothAdapter.ActionStateChanged
        //Intent.ActionAirplaneModeChanged
    })]
    public class ShinyBleAdapterStateBroadcastReceiver : BroadcastReceiver
    {
        readonly Lazy<IMessageBus> messageBus = new Lazy<IMessageBus>(() => ShinyHost.Resolve<IMessageBus>());
        //android.intent.action.AIRPLANE_MOD

        public override void OnReceive(Context context, Intent intent)
        {
            switch (intent.Action)
            {
                //case Intent.ActionAirplaneModeChanged:
                //    var mode = intent.GetBooleanExtra("state", false);
                //    if (mode)
                //        this.messageBus.Value.Publish(State.Off);
                //    break;

                case BluetoothAdapter.ActionConnectionStateChanged:
                    var newState = intent.GetIntExtra(BluetoothAdapter.ExtraState, -1);
                    this.messageBus.Value.Publish(newState);
                    break;
            }
        }
    }
}

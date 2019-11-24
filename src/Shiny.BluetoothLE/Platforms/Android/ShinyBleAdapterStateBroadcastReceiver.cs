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
    })]
    public class ShinyBleAdapterStateBroadcastReceiver : BroadcastReceiver
    {
        readonly Lazy<IMessageBus> messageBus = new Lazy<IMessageBus>(() => ShinyHost.Resolve<IMessageBus>());


        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action.Equals(BluetoothAdapter.ActionStateChanged))
            {
                var nativeState = (State)Enum.Parse(
                    typeof(State),
                    intent.GetStringExtra(BluetoothAdapter.ExtraState)
                );
                this.messageBus.Value.Publish(nativeState);
            }
        }
    }
}

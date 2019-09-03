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
        //BluetoothAdapter.ActionConnectionStateChanged,
        BluetoothAdapter.ActionStateChanged
    })]
    public class BluetoothAdapterBroadcastReceiver : BroadcastReceiver
    {
        readonly IMessageBus messageBus;
        readonly IBleAdapterDelegate adapterDelegate;


        public BluetoothAdapterBroadcastReceiver()
        {
            this.messageBus = ShinyHost.Resolve<IMessageBus>();
            this.adapterDelegate = ShinyHost.Resolve<IBleAdapterDelegate>();
        }


        public override void OnReceive(Context context, Intent intent) => this.Execute(async () =>
        {
            var nativeState = (State)Enum.Parse(typeof(State), intent.GetStringExtra(BluetoothAdapter.ExtraState));
            var access = nativeState.FromNative();
            this.adapterDelegate?.OnBleAdapterStateChanged(access);
            this.messageBus.Publish(access); // TODO: this needs to be more defined
        });
    }
}

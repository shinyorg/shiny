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
        readonly IMessageBus messageBus;
        readonly IBlePeripheralDelegate peripheralDelegate;


        public BluetoothDeviceBroadcastReceiver()
        {
            this.messageBus = ShinyHost.Resolve<IMessageBus>();
            this.peripheralDelegate = ShinyHost.Resolve<IBlePeripheralDelegate>();
        }


        public override void OnReceive(Context context, Intent intent) => this.Execute(async () =>
        {
            var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);

            switch (intent.Action)
            {
                case BluetoothDevice.ActionAclConnected:
                    //this.peripheralDelegate.OnConnected()
                    //this.messageBus.Publish()
                    break;

                case BluetoothDevice.ActionAclDisconnected:
                    // bus only
                    break;

                case BluetoothDevice.ActionBondStateChanged:
                    //this.messageBus.Publish()
                    break;

                case BluetoothDevice.ActionNameChanged:
                    // bus only
                    break;

                case BluetoothDevice.ActionPairingRequest:
                    // bus only
                    break;
            }
        });
    }
}

using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;


namespace Shiny.BluetoothLE.Central.Internals
{
    [BroadcastReceiver(
        Name = "com.shiny.bluetoothle.ShinyBleCentralBroadcastReceiver",
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
                //var nativeState = (State)Enum.Parse(typeof(State), intent.GetStringExtra(BluetoothAdapter.ExtraState));
                //var access = nativeState.FromNative();
                //this.sdelegate?.OnAdapterStateChanged(access);
                //this.messageBus.Publish(MessageBusNames.AdapterStateChanged, access);
            }
            else
            {
                var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);

                switch (intent.Action)
                {
                    case BluetoothDevice.ActionAclConnected:
                        //await this.sdelegate.OnConnected(null); // TODO:
                        //this.messageBus.Publish(MessageBusNames.PeripheralConnected, device);
                        break;

                    case BluetoothDevice.ActionAclDisconnected:
                        //this.messageBus.Publish(MessageBusNames.PeripheralDisconnected, device);
                        break;

                    case BluetoothDevice.ActionBondStateChanged:
                        //this.messageBus.Publish(MessageBusNames.PeripheralBondState, device);
                        break;

                    case BluetoothDevice.ActionNameChanged:
                        //this.messageBus.Publish(MessageBusNames.PeripheralNameChanged, device);
                        break;

                    case BluetoothDevice.ActionPairingRequest:
                        //this.messageBus.Publish(MessageBusNames.PeripheralPairingRequest, device);
                        break;
                }
            }
        });
    }
}

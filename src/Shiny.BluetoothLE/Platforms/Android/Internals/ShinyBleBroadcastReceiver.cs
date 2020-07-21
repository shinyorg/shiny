using System;
using System.Text;
using System.Threading.Tasks;
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
        BluetoothDevice.ActionNameChanged,
        BluetoothDevice.ActionBondStateChanged,
        BluetoothDevice.ActionPairingRequest
    })]
    public class ShinyBleBroadcastReceiver : BroadcastReceiver
    {
        readonly Lazy<CentralContext> shinyContext = ShinyHost.LazyResolve<CentralContext>();


        public override void OnReceive(Context context, Intent intent)
        {
            var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);

            switch (intent.Action)
            {
                case BluetoothDevice.ActionPairingRequest:
                    if (!intent.HasExtra(BluetoothDevice.ExtraPairingVariant))
                        return;

                    try
                    {
                        var pin = this.shinyContext.Value.GetPairingPinRequestForDevice(device);
                        if (!pin.IsEmpty() && device.SetPin(Encoding.UTF8.GetBytes(pin)))
                        {
                            device.SetPairingConfirmation(true);
                            this.InvokeAbortBroadcast();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Log.Write(ex);
                    }
                    break;

                default:
                    this.Execute(() =>
                    {
                        this.shinyContext.Value.DeviceEvent(intent.Action, device);
                        return Task.CompletedTask;
                    });
                    break;
            }
        }
    }
}

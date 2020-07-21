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
        public static string BlePairingFailed = nameof(BlePairingFailed);


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
                        Logging.Log.Write("BlePairing", "Will attempt to auto-pair");

                        var pin = this.shinyContext.Value.GetPairingPinRequestForDevice(device);
                        if (!pin.IsEmpty())
                        {
                            
                        }
                        else
                        { 
                            Logging.Log.Write("BlePairing", "Sending PIN " + pin);
                            var bytes = Encoding.UTF8.GetBytes(pin);
                            if (!device.SetPin(bytes))
                            {
                                Logging.Log.Write("BlePairing", "Auto-Pairing PIN failed");

                                this.shinyContext.Value.DeviceEvent(BlePairingFailed, device);
                            }
                            else
                            {
                                Logging.Log.Write("BlePairing", "Auto-Pairing PIN was sent successfully apparently");
                                this.InvokeAbortBroadcast();
                                device.SetPairingConfirmation(true);

                                this.shinyContext.Value.DeviceEvent(BluetoothDevice.ActionBondStateChanged, device);
                            }
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

using System;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Content;

namespace Shiny.BluetoothLE;


[BroadcastReceiver(
    Name = "org.shiny.bluetoothle.ShinyBleAdapterStateBroadcastReceiver",
    Enabled = true,
    Exported = true
)]
public class ShinyBleAdapterStateBroadcastReceiver : ShinyBroadcastReceiver
{
    public static Func<Intent, Task>? Process { get; set; }

    protected override async Task OnReceiveAsync(Context? context, Intent? intent)
    {
        if (Process != null && intent?.Action == BluetoothAdapter.ActionStateChanged)
            await Process(intent);
    }
}

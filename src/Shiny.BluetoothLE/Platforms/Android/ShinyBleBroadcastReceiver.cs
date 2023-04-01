using System;
using System.Threading.Tasks;
using Android.Content;

namespace Shiny.BluetoothLE;


[BroadcastReceiver(
    Name = BleManager.BroadcastReceiverName,
    Enabled = true,
    Exported = true
)]
public class ShinyBleBroadcastReceiver : ShinyBroadcastReceiver
{
    public static Func<Intent, Task>? Process { get; set; }


    protected override async Task OnReceiveAsync(Context? context, Intent? intent)
    {
        if (intent != null && Process != null)
            await Process(intent);
    }
}

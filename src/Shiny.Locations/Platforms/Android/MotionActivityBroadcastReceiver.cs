using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Gms.Location;

namespace Shiny.Locations;


[BroadcastReceiver(
    Name = MotionActivityManager.ReceiverName,
    Enabled = true,
    Exported = true
)]
public class MotionActivityBroadcastReceiver : ShinyBroadcastReceiver
{
    public static Func<ActivityTransitionResult, Task>? Process { get; set; }

    protected override async Task OnReceiveAsync(Context? context, Intent? intent)
    {
        if (intent != null && ActivityTransitionResult.HasResult(intent))
        {
            var result = ActivityTransitionResult.ExtractResult(intent);
            if (result != null && Process != null)
                await Process(result);
        }
    }
}

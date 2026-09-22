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
    public static Func<ActivityRecognitionResult, Task>? Process { get; set; }

    protected override async Task OnReceiveAsync(Context? context, Intent? intent)
    {
        if (intent != null && ActivityRecognitionResult.HasResult(intent))
        {
            var result = ActivityRecognitionResult.ExtractResult(intent);
            if (result != null && Process != null)
                await Process(result);
        }
    }
}

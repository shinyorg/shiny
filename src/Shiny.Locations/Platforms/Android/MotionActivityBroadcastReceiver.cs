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
    public static Func<ActivityRecognitionResult, Task>? ProcessUpdate { get; set; }
    public static Func<ActivityTransitionResult, Task>? ProcessTransitions { get; set; }


    protected override async Task OnReceiveAsync(Context? context, Intent? intent)
    {
        switch (intent?.Action)
        {
            case MotionActivityManager.TransitionIntentAction:
                if (ActivityTransitionResult.HasResult(intent))
                {
                    var result = ActivityTransitionResult.ExtractResult(intent);
                    if (result != null && ProcessTransitions != null)
                        await ProcessTransitions(result);
                }
                break;

            case MotionActivityManager.UpdateIntentAction:
                if (ActivityRecognitionResult.HasResult(intent))
                {
                    var result = ActivityRecognitionResult.ExtractResult(intent);
                    if (result != null && ProcessUpdate != null)
                        await ProcessUpdate(result);
                }
                break;
        }
    }
}

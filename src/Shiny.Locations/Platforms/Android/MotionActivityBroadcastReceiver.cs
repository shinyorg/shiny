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
    public const string ReceiverName = nameof(MotionActivityBroadcastReceiver);

    public static Func<ActivityTransitionResult, Task>? ProcessTransition { get; set; }
    public static Func<ActivityRecognitionResult, Task>? ProcessRecognition { get; set; }


    protected override async Task OnReceiveAsync(Context? context, Intent? intent)
    {
        if (ActivityTransitionResult.HasResult(intent) && ProcessTransition != null)
        {
            var result = ActivityTransitionResult.ExtractResult(intent);
            await ProcessTransition(result);
        }

        if (ActivityRecognitionResult.HasResult(intent) && ProcessRecognition != null)
        {
            var result = ActivityRecognitionResult.ExtractResult(intent);
            await ProcessRecognition(result);
        }
    }
}
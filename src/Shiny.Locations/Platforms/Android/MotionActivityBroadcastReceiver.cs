using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Location;


namespace Shiny.Locations
{
    [BroadcastReceiver(
        Name = MotionActivityManagerImpl.ReceiverName,
        Enabled = true,
        Exported = true
    )]
    [IntentFilter(new[] {
        MotionActivityManagerImpl.IntentAction
    })]
    public class MotionActivityBroadcastReceiver : ShinyBroadcastReceiver
    {
        public const string ReceiverName = nameof(MotionActivityBroadcastReceiver);
        public static Func<ActivityRecognitionResult, Task>? Process { get; set; }


        protected override async Task OnReceiveAsync(Context? context, Intent? intent)
        {
            if (ActivityRecognitionResult.HasResult(intent) && Process != null)
            {
                var result = ActivityRecognitionResult.ExtractResult(intent);
                await Process(result);
            }
        }
    }
}
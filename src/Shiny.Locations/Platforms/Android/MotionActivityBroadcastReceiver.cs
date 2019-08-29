using System;
using Android.Content;
using Android.Gms.Location;
using Android.Database.Sqlite;
using Shiny.Logging;


namespace Shiny.Locations
{
    public class MotionActivityBroadcastReceiver : BroadcastReceiver
    {
        readonly IMessageBus messageBus;
        //https://developer.android.com/training/data-storage/sqlite

        public MotionActivityBroadcastReceiver()
        {
            this.messageBus = ShinyHost.Resolve<IMessageBus>();
        }


        public override void OnReceive(Context context, Intent intent)
        {
            try
            {
                if (ActivityRecognitionResult.HasResult(intent))
                {
                    var result = ActivityRecognitionResult.ExtractResult(intent);
                    foreach (var activity in result.ProbableActivities)
                    {
                        //activity.Confidence
                    }
                    //result.GetActivityConfidence()
                    //result.ProbableActivities
                    //result.Time
                    //foreach (var act in result.ProbableActivities)
                    //{
                    //    //act.Confidence
                    //    //act.Type
                    //}
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}
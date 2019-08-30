using System;
using Android.Content;
using Android.Gms.Location;


namespace Shiny.Locations
{
    public class MotionActivityBroadcastReceiver : BroadcastReceiver
    {
        readonly IMessageBus messageBus;
        readonly AndroidSqliteDatabase database;
        //https://developer.android.com/training/data-storage/sqlite


        public MotionActivityBroadcastReceiver()
        {
            this.messageBus = ShinyHost.Resolve<IMessageBus>();
            this.database = ShinyHost.Resolve<AndroidSqliteDatabase>();
        }


        public override void OnReceive(Context context, Intent intent) => this.Execute(async () =>
        {
            // DELETE FROM motion_activity WHERE Timestamp < DateTimeOffset.UtcNow.AddDays(-30).Ticks
            if (ActivityRecognitionResult.HasResult(intent))
            {
                var result = ActivityRecognitionResult.ExtractResult(intent);
                foreach (var activity in result.ProbableActivities)
                {
                    var confidence = 0;
                    var type = (int) MotionActivityType.Unknown;
                    var timestamp = DateTime.UtcNow.Ticks;

                    await this.database.ExecuteNonQuery(
                        $"INSERT INTO motion_activity(Event, Confidence, Timestamp) VALUES ({type}, {confidence}, {timestamp})"
                    );
                }
            }
        });
    }
}
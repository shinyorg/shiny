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
            if (!ActivityRecognitionResult.HasResult(intent))
                return;

            var result = ActivityRecognitionResult.ExtractResult(intent);
            var type = MotionActivityType.Unknown;

            foreach (var activity in result.ProbableActivities)
            {
                switch (activity.Type)
                {
                    case DetectedActivity.InVehicle:
                        type |= MotionActivityType.Automotive;
                        break;

                    case DetectedActivity.OnBicycle:
                        type |= MotionActivityType.Cycling;
                        break;

                    case DetectedActivity.OnFoot:
                        type |= MotionActivityType.Walking;
                        break;

                    case DetectedActivity.Running:
                        type |= MotionActivityType.Running;
                        break;

                    case DetectedActivity.Still:
                        type |= MotionActivityType.Stationary;
                        break;

                    case DetectedActivity.Walking:
                        type |= MotionActivityType.Walking;
                        break;
                }
            }
            var confidence = this.ToConfidence(result.MostProbableActivity.Confidence);
            var timestamp = DateTime.UtcNow.Ticks;

            await this.database.ExecuteNonQuery(
                $"INSERT INTO motion_activity(Event, Confidence, Timestamp) VALUES ({type}, {confidence}, {timestamp})"
            );
            this.messageBus.Publish(new MotionActivityEvent(type, MotionActivityConfidence.High, DateTimeOffset.UtcNow));
        });


        protected virtual MotionActivityConfidence ToConfidence(int value)
        {
            if (value >= 70)
                return MotionActivityConfidence.High;

            if (value >= 40)
                return MotionActivityConfidence.Medium;

            return MotionActivityConfidence.Low;
        }
    }
}
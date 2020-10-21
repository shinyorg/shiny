using System;
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
        readonly IMessageBus messageBus;
        readonly AndroidSqliteDatabase database;


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
                    case DetectedActivity.Walking:
                        type |= MotionActivityType.Walking;
                        break;

                    case DetectedActivity.Running:
                        type |= MotionActivityType.Running;
                        break;

                    case DetectedActivity.Still:
                        type |= MotionActivityType.Stationary;
                        break;
                }
            }
            var confidence = this.ToConfidence(result.MostProbableActivity.Confidence);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            await this.database.ExecuteNonQuery(
                $"INSERT INTO motion_activity(Event, Confidence, Timestamp) VALUES ({(int)type}, {(int)confidence}, {timestamp})"
            );
            this.messageBus.Publish(new MotionActivityEvent(type, confidence, DateTimeOffset.UtcNow));
        });


        protected virtual MotionActivityConfidence ToConfidence(int value)
        {
            if (value >= MotionActivityManagerImpl.HighConfidenceValue)
                return MotionActivityConfidence.High;

            if (value >= MotionActivityManagerImpl.MediumConfidenceValue)
                return MotionActivityConfidence.Medium;

            return MotionActivityConfidence.Low;
        }
    }
}
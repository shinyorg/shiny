using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Locations;


namespace Shiny.TripTracker.Internals
{
    public class TripTrackerGpsDelegate : NotifyPropertyChanged, IGpsDelegate
    {
        readonly ITripTrackerManager manager;
        readonly IMotionActivityManager activityManager;
        readonly IDataService dataService;
        readonly IEnumerable<ITripTrackerDelegate> delegates;


        public TripTrackerGpsDelegate(ITripTrackerManager manager,
                                      IMotionActivityManager activityManager,
                                      IDataService dataService,
                                      IEnumerable<ITripTrackerDelegate> delegates)
        {
            this.manager = manager;
            this.activityManager = activityManager;
            this.dataService = dataService;
            this.delegates = delegates;
        }


        int? currentTripId;
        public int? CurrentTripId
        {
            get => this.currentTripId;
            set => this.Set(ref this.currentTripId, value);
        }


        public async Task OnReading(IGpsReading reading)
        {
            if (this.manager.TrackingType == null)
                return;

            // TODO: watch for sensor blips?
            var n = nameof(TripTrackerGpsDelegate);
            var currentMotion = await this.GetLastActivity();
            var track = this.IsTracked(currentMotion);
            Logging.Log.Write(n, $"Current Motion: {currentMotion?.Types.ToString() ?? "Empty"} - Track: {track} - Current: {this.CurrentTripId}");

            if (this.CurrentTripId == null)
            {
                if (!track)
                    return;

                var trip = new Trip
                {
                    StartLatitude = reading.Position.Latitude,
                    StartLongitude = reading.Position.Longitude,
                    Type = this.manager.TrackingType.Value,
                    DateStarted = DateTimeOffset.UtcNow
                };
                await this.dataService.Save(trip);
                this.CurrentTripId = trip.Id;

                await this.dataService.Checkin(this.CurrentTripId.Value, reading);
                await this.delegates.RunDelegates(x => x.OnTripStart(trip));
            }
            else
            {
                await this.dataService.Checkin(this.CurrentTripId.Value, reading);

                if (!track)
                {
                    // stop trip
                    var trip = await this.dataService.GetTrip(this.CurrentTripId.Value);
                    trip.DateFinished = DateTimeOffset.UtcNow;
                    trip.AverageSpeedMetersPerHour = await this.dataService.GetTripTotalDistanceInMeters(trip.Id);
                    trip.TotalDistanceMeters = await this.dataService.GetTripTotalDistanceInMeters(trip.Id);
                    trip.StartLatitude = reading.Position.Longitude;
                    trip.EndLatitude = reading.Position.Latitude;

                    this.CurrentTripId = null;
                    await this.dataService.Save(trip);
                    await this.delegates.RunDelegates(x => x.OnTripEnd(trip));
                }
            }
        }


        async Task<MotionActivityEvent> GetLastActivity()
        {
            var ts = TimeSpan.FromMinutes(5);
            if (this.CurrentTripId != null)
            {
                var trip = await this.dataService.GetTrip(this.CurrentTripId.Value);
                ts = DateTimeOffset.UtcNow.Subtract(trip.DateStarted);
            }
            return await this.activityManager.GetCurrentActivity(ts);
        }


        bool IsTracked(MotionActivityEvent? e)
        {
            if (e == null)
                return this.CurrentTripId != null;

            switch (this.manager.TrackingType.Value)
            {
                case TripTrackingType.Stationary:
                    return e.Types.HasFlag(MotionActivityType.Stationary);

                case TripTrackingType.Cycling:
                    return e.Types.HasFlag(MotionActivityType.Cycling);

                case TripTrackingType.Running:
                    return e.Types.HasFlag(MotionActivityType.Running);

                case TripTrackingType.Walking:
                    return e.Types.HasFlag(MotionActivityType.Walking);

                case TripTrackingType.Automotive:
                    return e.Types.HasFlag(MotionActivityType.Automotive);

                case TripTrackingType.Exercise:
                    return e.Types.HasFlag(MotionActivityType.Cycling) ||
                           e.Types.HasFlag(MotionActivityType.Running) ||
                           e.Types.HasFlag(MotionActivityType.Walking);

                case TripTrackingType.OnFoot:
                    return e.Types.HasFlag(MotionActivityType.Running) ||
                           e.Types.HasFlag(MotionActivityType.Walking);

                default:
                    throw new Exception("Invalid Flag");
            }
        }
    }
}

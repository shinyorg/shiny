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
            if (this.manager.TrackingActivityType == null)
                return;

            // TODO: watch for sensor blips?
            var n = nameof(TripTrackerGpsDelegate);
            var currentMotion = await this.activityManager.GetCurrentActivity(TimeSpan.FromMinutes(5));
            var track = currentMotion != null && currentMotion.Types.HasFlag(this.manager.TrackingActivityType);
            Logging.Log.Write(n, $"Current Motion: {currentMotion?.Types.ToString() ?? "Empty"} - Track: {track} - Current: {this.CurrentTripId}");

            if (this.CurrentTripId == null)
            {
                if (!track)
                    return;

                var trip = new Trip
                {
                    StartLatitude = reading.Position.Latitude,
                    StartLongitude = reading.Position.Longitude,
                    TripType = this.manager.TrackingActivityType.Value,
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


        //bool IsTracked(MotionActivityEvent? e)
        //{
        //    if (e == null)
        //        return false;

        //    switch (this.manager.TrackingActivityType)
        //    {
        //        case MotionActivityType.Stationary:
        //            return e.Types.HasFlag(MotionActivityType.Stationary) ||
        //                   e.Types.HasFlag(MotionActivityType.Unknown);

        //        default:
        //            return e.Types.HasFlag(this.manager.TrackingActivityType);
        //    }
        //}


        //bool IsTracked(MotionActivityEvent? e)
        //{
        //    var at = TripTrackingType.Cycling;
        //    if (e == null)
        //        return false;

        //    switch (at)
        //    {
        //        case TripTrackingType.Stationary:
        //            return e.Types.HasFlag(MotionActivityType.Stationary) ||
        //                   e.Types.HasFlag(MotionActivityType.Unknown);

        //        case TripTrackingType.Cycling:
        //            return e.Types.HasFlag(MotionActivityType.Cycling);

        //        case TripTrackingType.Running:
        //            return e.Types.HasFlag(MotionActivityType.Running);

        //        case TripTrackingType.Walking:
        //            return e.Types.HasFlag(MotionActivityType.Walking);

        //        case TripTrackingType.Automotive:
        //            return e.Types.HasFlag(MotionActivityType.Automotive);

        //        case TripTrackingType.Exercise:
        //            return e.Types.HasFlag(MotionActivityType.Cycling);

        //        case TripTrackingType.OnFoot:
        //            return e.Types.HasFlag(MotionActivityType.Cycling);

        //        default:
        //            throw new Exception("Invalid Flag");
        //    }
        //}
    }
}

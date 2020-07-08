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
            if (this.manager.TrackingActivityTypes == null)
                return;

            // TODO: if the starting trip activity != the current activity, stop current trip and create a new one
            var currentMotion = await this.activityManager.GetCurrentActivity(TimeSpan.FromSeconds(60));
            var track = currentMotion != null && IsTracked(this.manager.TrackingActivityTypes.Value, currentMotion.Types);

            if (this.CurrentTripId == null)
            {
                if (!track)
                    return;

                // TODO: need first flag from enum of incoming
                var trip = new Trip
                {
                    StartLatitude = reading.Position.Latitude,
                    StartLongitude = reading.Position.Longitude,
                    TripType = MotionActivityType.Automotive,
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
                    trip.TotalDistanceMeters = await this.CalculateDistanceInMeters();
                    trip.StartLatitude = reading.Position.Longitude;
                    trip.EndLatitude = reading.Position.Latitude;

                    this.CurrentTripId = null;
                    await this.dataService.Save(trip);
                    await this.delegates.RunDelegates(x => x.OnTripEnd(trip));
                }
            }
        }


        async Task<double> CalculateDistanceInMeters()
        {
            var total = 0d;
            Position? last = null;
            var checkins = await this.dataService.GetCheckinsByTrip(this.CurrentTripId.Value);

            foreach (var checkin in checkins)
            {
                var current = new Position(checkin.Latitude, checkin.Longitude);
                if (last != null)
                    total += last.GetDistanceTo(current).TotalMeters;

                last = current;
            }
            return total;
        }


        static bool IsTracked(MotionActivityType incoming, MotionActivityType types)
        {
            if (types.HasFlag(MotionActivityType.Automotive) && incoming.HasFlag(MotionActivityType.Automotive))
                return true;

            if (types.HasFlag(MotionActivityType.Cycling) && incoming.HasFlag(MotionActivityType.Cycling))
                return true;

            if (types.HasFlag(MotionActivityType.Running) && incoming.HasFlag(MotionActivityType.Running))
                return true;

            if (types.HasFlag(MotionActivityType.Walking) && incoming.HasFlag(MotionActivityType.Walking))
                return true;

            return false;
        }
    }
}

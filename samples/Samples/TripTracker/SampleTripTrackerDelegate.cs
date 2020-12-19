using Humanizer;
using Shiny;
using Shiny.TripTracker;
using System;
using System.Threading.Tasks;


namespace Samples.TripTracker
{
    public class SampleTripTrackerDelegate : ITripTrackerDelegate, IShinyStartupTask
    {
        const string N_TITLE = "Shiny Trip";
        readonly ITripTrackerManager manager;
        readonly AppNotifications notifications;


        public SampleTripTrackerDelegate(ITripTrackerManager manager, AppNotifications notifications)
        {
            this.manager = manager;
            this.notifications = notifications;
        }


        public Task OnTripStart(Trip trip) => this.notifications.Send(
            this.GetType(),
            true,
            N_TITLE,
            $"Starting a new {this.manager.TrackingType.Value} trip"
        );


        public async Task OnTripEnd(Trip trip)
        {
            var km = Math.Round(Distance.FromMeters(trip.TotalDistanceMeters).TotalKilometers, 0);
            var avgSpeed = Math.Round(Distance.FromMeters(trip.AverageSpeedMetersPerHour).TotalKilometers, 0);
            var time = (trip.DateFinished - trip.DateStarted).Value.Humanize();

            await this.notifications.Send(
                this.GetType(),
                false,
                N_TITLE,
                $"You just finished a trip that was {km} km and took {time} with an average speed of {avgSpeed} km"
            );
        }


        public void Start()
            => this.notifications.Register(this.GetType(), true, "Trip Tracker");
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Shiny.Locations;
using Shiny.TripTracker.Internals;
using Xunit;


namespace Shiny.TripTracker.Tests
{
    public class TripTrackerGpsDelegateTests
    {
        readonly Mock<IDataService> data;
        readonly Mock<IMotionActivityManager> activityManager;
        readonly Mock<ITripTrackerManager> manager;
        readonly TripTrackerGpsDelegate gpsDelegate;
        readonly List<TripCheckin> checkins = new List<TripCheckin>();
        MotionActivityType? tracking = MotionActivityType.Automotive;
        MotionActivityType currentMotionType = MotionActivityType.Automotive;


        public TripTrackerGpsDelegateTests()
        {
            this.manager = new Mock<ITripTrackerManager>();
            this.activityManager = new Mock<IMotionActivityManager>();
            this.data = new Mock<IDataService>();

            this.manager
                .Setup(x => x.TrackingActivityTypes)
                .Returns(() => this.tracking);

            this.activityManager
                .Setup(x => x.Query(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
                .Returns(() => Task.FromResult<IList<MotionActivityEvent>>(new List<MotionActivityEvent> 
                {
                    new MotionActivityEvent(
                        this.currentMotionType, 
                        MotionActivityConfidence.High, 
                        DateTimeOffset.UtcNow
                    )
                }));


            this.data
                .Setup(x => x.Checkin(
                    It.IsAny<int>(), 
                    It.IsAny<IGpsReading>()
                ))
                .Returns(Task.CompletedTask);

            this.data
                .Setup(x => x.GetCheckinsByTrip(It.IsAny<int>()))
                .Returns(() => Task.FromResult<IList<TripCheckin>>(this.checkins));

            this.data
                .Setup(x => x.Checkin(It.IsAny<int>(), It.IsAny<IGpsReading>()))
                .Callback<int, IGpsReading>((tripId, reading) => this.checkins.Add(new TripCheckin
                {
                    TripId = tripId,
                    Direction = reading.Heading,
                    Latitude = reading.Position.Latitude,
                    Longitude = reading.Position.Longitude,
                    DateCreated = DateTimeOffset.Now
                }))
                .Returns(Task.CompletedTask);

            this.gpsDelegate = new TripTrackerGpsDelegate(
                this.manager.Object,
                this.activityManager.Object,
                this.data.Object,
                null
            );
        }


        [Fact]
        public async Task NewTripCreated()
        {
            Trip? result = null;
            this.data
                .Setup(x => x.Save(It.IsAny<Trip>()))
                .Callback<Trip>(x => result = x)
                .Returns(Task.CompletedTask);

            await this.gpsDelegate.OnReading(GpsReading.Create(1, 1));
            result.Should().NotBeNull();
            result.DateFinished.Should().BeNull();
        }


        [Fact]
        public async Task TripClosedWhenActivityChanges()
        {
            var trip = new Trip
            {
                Id = 1,
                TripType = MotionActivityType.Automotive
            };
            this.gpsDelegate.CurrentTripId = 1;
            this.data
                .Setup(x => x.GetTrip(It.IsAny<int>()))
                .Returns(Task.FromResult(trip));

            this.currentMotionType = MotionActivityType.Cycling;
            await this.gpsDelegate.OnReading(GpsReading.Create(1, 1));
            trip.DateFinished.Should().NotBeNull();
        }


        [Fact]
        public async Task TripDistanceCalculatedProperly()
        {
        }
    }
}

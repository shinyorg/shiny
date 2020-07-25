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
        Trip? trip;


        public TripTrackerGpsDelegateTests()
        {
            this.manager = new Mock<ITripTrackerManager>();
            this.activityManager = new Mock<IMotionActivityManager>();
            this.data = new Mock<IDataService>();

            this.manager
                .Setup(x => x.TrackingActivityType)
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
                .Setup(x => x.Save(It.IsAny<Trip>()))
                .Callback<Trip>(x =>
                {
                    x.Id = 1;
                    this.trip = x;
                })
                .Returns(Task.CompletedTask);

            this.data
                .Setup(x => x.GetTrip(It.IsAny<int>()))
                .Returns(() => Task.FromResult(this.trip));

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
            await this.gpsDelegate.OnReading(GpsReading.Create(1, 1));
            this.gpsDelegate.CurrentTripId.Should().NotBeNull();
            this.gpsDelegate.CurrentTripId.Should().Be(1);
            this.trip.Should().NotBeNull();
            this.trip.TripType.HasFlag(MotionActivityType.Automotive).Should().BeTrue();
            this.trip.DateFinished.Should().BeNull();
        }


        [Fact]
        public async Task TripClosedWhenActivityChanges()
        {
            this.trip = new Trip
            {
                Id = 1,
                TripType = MotionActivityType.Automotive
            };
            this.gpsDelegate.CurrentTripId = 1;
            this.currentMotionType = MotionActivityType.Cycling;
            await this.gpsDelegate.OnReading(GpsReading.Create(1, 1));
            this.trip.DateFinished.Should().NotBeNull();
        }


        [Fact]
        public async Task TripDistanceCalculatedProperly()
        {
            // start new and log trip
            await this.gpsDelegate.OnReading(GpsReading.Create(43.950534, -79.013066));
            await this.gpsDelegate.OnReading(GpsReading.Create(43.9378078,-79.0077017));
            await this.gpsDelegate.OnReading(GpsReading.Create(43.9315338,-79.0046657));
            await this.gpsDelegate.OnReading(GpsReading.Create(43.9199038,-79.0000827));
            await this.gpsDelegate.OnReading(GpsReading.Create(43.9123987,-79.0044507));
            await this.gpsDelegate.OnReading(GpsReading.Create(43.8894793,-78.9998306));
            await this.gpsDelegate.OnReading(GpsReading.Create(43.8769798,-78.9808417));

            // finish trip
            this.currentMotionType = MotionActivityType.Cycling;
            await this.gpsDelegate.OnReading(GpsReading.Create(43.8744028, -78.9806041));

            // assert
            this.checkins.Count.Should().Be(8);
            this.trip.Should().NotBeNull();
            this.trip.DateFinished.Should().NotBeNull();
            this.trip.TotalDistanceMeters.Should().Be(9398.357555710918);
        }
    }
}

using System;
using System.Threading.Tasks;
using Moq;
using Shiny.Locations;
using Shiny.TripTracker;
using Shiny.TripTracker.Internals;
using Xunit;


namespace Shiny.Tests.TripTracker
{
    public class TripTrackerGpsDelegateTests
    {
        readonly Mock<IDataService> data;
        readonly Mock<IMotionActivityManager> activityManager;
        readonly Mock<ITripTrackerManager> manager;
        readonly Mock<IGpsReading> gpsReading;
        readonly TripTrackerGpsDelegate gpsDelegate;


        public TripTrackerGpsDelegateTests()
        {
            this.manager = new Mock<ITripTrackerManager>();
            this.activityManager = new Mock<IMotionActivityManager>();
            this.data = new Mock<IDataService>();
            this.gpsReading = new Mock<IGpsReading>();

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
        }


        [Fact]
        public async Task TripClosedWhenActivityChanges()
        {

        }


        [Fact]
        public async Task TripDistanceCalcuatedProperly()
        {
        }
    }
}

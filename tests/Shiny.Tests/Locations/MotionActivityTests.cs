//#if NETCOREAPP
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using FluentAssertions;
//using Moq;
//using Shiny.Locations;
//using Xunit;


//namespace Shiny.Tests.Locations
//{
//    public class MotionActivityTests
//    {
//        readonly Mock<IMotionActivityManager> activityManager;


//        public MotionActivityTests()
//        {
//            this.activityManager = new Mock<IMotionActivityManager>();
//            this.activityManager
//                .Setup(x => x.Query(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset?>()))
//                .Returns(Task.FromResult(this.Data));
//        }


//        public IList<MotionActivityEvent> Data => new List<MotionActivityEvent>
//        {
//            new MotionActivityEvent(MotionActivityType.Automotive, MotionActivityConfidence.Low, DateTimeOffset.UtcNow.AddDays(-3)),
//            new MotionActivityEvent(MotionActivityType.Automotive, MotionActivityConfidence.Low, DateTimeOffset.UtcNow.AddDays(-3))
//        };


//        [Theory(Skip = "TODO")]
//        [InlineData(5)]
//        public async Task GetCurrentActivityTest(int maxAgeSeconds)
//        {
//            var result = await this.activityManager.Object.GetCurrentActivity(TimeSpan.FromSeconds(maxAgeSeconds));
//        }


//        //[Theory]
//        //[InlineData()]
//        //public async Task GetTotalsForRangeTest()
//        //{
//        //    this.activityManager.Object.GetTotalsForRange();
//        //}

//        //[Theory(Skip = "TODO")]
//        //[InlineData(MotionActivityConfidence.Low, 0)]
//        //[InlineData(MotionActivityConfidence.Medium, 0)]
//        //[InlineData(MotionActivityConfidence.High, 0)]
//        //public async Task GetTimeBlocksTest(MotionActivityConfidence confidence, int expectedAutoSeconds)
//        //{
//        //    var result = await this.activityManager.Object.GetTimeBlocksForRange(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, confidence);

//        //    var auto = result.FirstOrDefault(x => x.Type == MotionActivityType.Automotive);
//        //    if (expectedAutoSeconds == 0)
//        //        auto.Should().Be(null);
//        //    else
//        //    {
//        //        auto.Should().NotBeNull();
//        //        //auto.Start
//        //        //    auto.End
//        //    }
//        //}
//    }
//}
//#endif
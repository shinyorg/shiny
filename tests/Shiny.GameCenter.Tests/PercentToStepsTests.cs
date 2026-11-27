using Shiny.GameCenter.Infrastructure;
using Xunit;

namespace Shiny.GameCenter.Tests;


public class PercentToStepsTests
{
    [Theory]
    [InlineData(33.0, 3, 1)]
    [InlineData(33.333333, 3, 1)]
    [InlineData(100d / 3, 3, 1)]
    [InlineData(66.0, 3, 2)]
    [InlineData(66.67, 3, 2)]
    [InlineData(200d / 3, 3, 2)]
    [InlineData(100.0, 3, 3)]
    [InlineData(99.0, 100, 99)]
    [InlineData(0.0, 5, 0)]
    [InlineData(57.3, 1000, 573)]
    [InlineData(0.7, 1000, 7)]
    [InlineData(150.0, 10, 10)]
    [InlineData(-5.0, 10, 0)]
    public void Converts(double percent, int totalSteps, int expected)
        => Assert.Equal(expected, GameCenterManager.PercentToSteps(percent, totalSteps));


    [Theory]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(10)]
    [InlineData(99)]
    [InlineData(100)]
    public void RoundTripsAtFullAndWholePercentPrecision(int totalSteps)
    {
        for (var steps = 0; steps <= totalSteps; steps++)
        {
            var percent = steps * 100d / totalSteps;
            Assert.Equal(steps, GameCenterManager.PercentToSteps(percent, totalSteps));
            Assert.Equal(steps, GameCenterManager.PercentToSteps(Math.Round(percent), totalSteps));
        }
    }


    [Theory]
    [InlineData(101)]
    [InlineData(1000)]
    [InlineData(12345)]
    public void RoundTripsAtFullPrecisionAboveOneHundredSteps(int totalSteps)
    {
        for (var steps = 0; steps <= totalSteps; steps++)
            Assert.Equal(steps, GameCenterManager.PercentToSteps(steps * 100d / totalSteps, totalSteps));
    }
}

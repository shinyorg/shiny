using Shiny.Beacons.Infrastructure;

namespace Shiny.Beacons.Tests;


public class DistanceEstimatorTests
{
    [Fact]
    public void PathLoss_AtCalibrationPower_IsOneMetre()
    {
        var estimator = new PathLossDistanceEstimator();

        Assert.Equal(1.0, estimator.Estimate(-59, -59), 3);
    }


    [Fact]
    public void PathLoss_WeakerSignalIsFurtherAway()
    {
        var estimator = new PathLossDistanceEstimator();

        var near = estimator.Estimate(-59, -59);
        var mid = estimator.Estimate(-70, -59);
        var far = estimator.Estimate(-85, -59);

        Assert.True(near < mid);
        Assert.True(mid < far);
    }


    [Fact]
    public void PathLoss_SixDbLossDoublesTheDistance()
    {
        // free space: every 6 dB of loss is a doubling of distance
        var estimator = new PathLossDistanceEstimator(2.0);

        var one = estimator.Estimate(-59, -59);
        var two = estimator.Estimate(-65, -59);

        Assert.Equal(2.0, two / one, 1);
    }


    [Fact]
    public void PathLoss_HigherExponentReadsCloser()
    {
        // a cluttered room attenuates faster, so the same RSSI means a shorter distance
        var free = new PathLossDistanceEstimator(2.0).Estimate(-80, -59);
        var cluttered = new PathLossDistanceEstimator(3.5).Estimate(-80, -59);

        Assert.True(cluttered < free);
    }


    [Fact]
    public void PathLoss_RejectsNonPositiveExponent()
        => Assert.Throws<InvalidOperationException>(() => new PathLossDistanceEstimator(0).Estimate(-70, -59));


    [Fact]
    public void CurveFit_WeakerSignalIsFurtherAway()
    {
        var estimator = new CurveFitDistanceEstimator();

        Assert.True(estimator.Estimate(-59, -59) < estimator.Estimate(-75, -59));
        Assert.True(estimator.Estimate(-75, -59) < estimator.Estimate(-90, -59));
    }


    public static TheoryData<IBeaconDistanceEstimator> Estimators => new()
    {
        new PathLossDistanceEstimator(),
        new CurveFitDistanceEstimator()
    };


    [Theory]
    [MemberData(nameof(Estimators))]
    public void ZeroRssi_IsReportedAsUnknown(IBeaconDistanceEstimator estimator)
        // a zero RSSI means the platform had no measurement, not an extremely strong signal
        => Assert.True(estimator.Estimate(0, -59) < 0);


    [Fact]
    public void ProximityThresholds_FollowAppleBoundaries()
    {
        var options = new BeaconRangingOptions();

        // The pre-revival CalculateProximity compared a distance to 6E-6 and 0.5E-6, which is not a
        // distance in any unit - everything came back Immediate.
        Assert.Equal(Proximity.Immediate, options.ToProximity(0.1));
        Assert.Equal(Proximity.Immediate, options.ToProximity(0.49));
        Assert.Equal(Proximity.Near, options.ToProximity(0.5));
        Assert.Equal(Proximity.Near, options.ToProximity(2.99));
        Assert.Equal(Proximity.Far, options.ToProximity(3.0));
        Assert.Equal(Proximity.Far, options.ToProximity(50));
        Assert.Equal(Proximity.Unknown, options.ToProximity(-1));
        Assert.Equal(Proximity.Unknown, options.ToProximity(Double.NaN));
    }


    [Fact]
    public void ProximityThresholds_AreConfigurable()
    {
        var options = new BeaconRangingOptions { ImmediateThreshold = 1.0, NearThreshold = 10.0 };

        Assert.Equal(Proximity.Immediate, options.ToProximity(0.9));
        Assert.Equal(Proximity.Near, options.ToProximity(5));
        Assert.Equal(Proximity.Far, options.ToProximity(11));
    }
}

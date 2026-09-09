using Microsoft.Extensions.Time.Testing;
using Shiny.Beacons.Infrastructure;

namespace Shiny.Beacons.Tests;


public class RssiFilterTests
{
    readonly FakeTimeProvider clock = new(DateTimeOffset.Parse("2026-09-09T12:00:00Z"));


    RssiFilter Create(double trim = 0.1) => new(TimeSpan.FromSeconds(20), trim, this.clock);


    [Fact]
    public void Empty_HasNoValue()
    {
        var filter = this.Create();

        Assert.Null(filter.Value);
        Assert.Null(filter.Latest);
        Assert.Equal(0, filter.Count);
    }


    [Fact]
    public void SingleSample_IsItsOwnAverage()
    {
        var filter = this.Create();
        filter.Add(-70);

        Assert.Equal(-70, filter.Value);
        Assert.Equal(-70, filter.Latest);
    }


    [Fact]
    public void SamplesOutsideTheWindow_AreDropped()
    {
        var filter = this.Create();
        filter.Add(-90);

        this.clock.Advance(TimeSpan.FromSeconds(21));
        filter.Add(-60);

        // the -90 has aged out, so it must not drag the average down
        Assert.Equal(-60, filter.Value);
        Assert.Equal(1, filter.Count);
    }


    [Fact]
    public void OutliersAreTrimmedBeforeAveraging()
    {
        var filter = this.Create();

        // eight steady readings around -70, plus a reflection and a dropout at the extremes
        int[] samples = [-70, -71, -69, -70, -72, -68, -70, -71, -30, -100];
        foreach (var sample in samples)
            filter.Add(sample);

        var value = filter.Value;
        Assert.NotNull(value);

        // the raw mean is dragged well off by the two outliers; the trimmed mean is not
        var rawMean = samples.Average();
        Assert.True(Math.Abs(value!.Value - rawMean) > 1.0, $"trimmed {value} should differ from raw mean {rawMean}");
        Assert.InRange(value.Value, -73, -67);
    }


    [Fact]
    public void TwoSamples_AreAveragedWithoutTrimming()
    {
        var filter = this.Create();
        filter.Add(-60);
        filter.Add(-80);

        // trimming a two-sample set would throw one of them away for no reason
        Assert.Equal(-70, filter.Value);
    }


    [Fact]
    public void AggressiveTrim_StillLeavesASample()
    {
        var filter = this.Create(0.4);
        foreach (var sample in new[] { -50, -60, -70, -80, -90 })
            filter.Add(sample);

        Assert.NotNull(filter.Value);
        Assert.Equal(-70, filter.Value);
    }


    [Fact]
    public void Clear_EmptiesTheWindow()
    {
        var filter = this.Create();
        filter.Add(-70);
        filter.Clear();

        Assert.Null(filter.Value);
    }


    [Fact]
    public void RejectsInvalidConfiguration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RssiFilter(TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RssiFilter(TimeSpan.FromSeconds(1), -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RssiFilter(TimeSpan.FromSeconds(1), 0.5));
    }


    [Fact]
    public void FilteringMakesDistanceStable()
    {
        // The point of the whole thing: the same noisy signal, with and without the filter, run
        // through the same estimator. The pre-revival module had no filter at all.
        var estimator = new PathLossDistanceEstimator();
        int[] noisy = [-65, -75, -70, -80, -62, -71, -69, -73, -70, -68];

        var unfiltered = noisy.Select(x => estimator.Estimate(x, -59)).ToList();
        var filter = this.Create();

        var filtered = new List<double>();
        foreach (var sample in noisy)
        {
            filter.Add(sample);
            filtered.Add(estimator.Estimate(filter.Value!.Value, -59));
        }

        var unfilteredSpread = unfiltered.Max() - unfiltered.Min();
        var filteredSpread = filtered.Skip(3).Max() - filtered.Skip(3).Min();

        Assert.True(filteredSpread < unfilteredSpread / 2, $"filtered spread {filteredSpread:N2}m should be far tighter than unfiltered {unfilteredSpread:N2}m");
    }
}

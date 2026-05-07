using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shiny;
using Shiny.Locations;
using Xunit;

namespace Shiny.Tests;

public class TestGpsDelegate : GpsDelegate
{
    public TestGpsDelegate() : base(NullLogger.Instance) { }

    public List<GpsReading> FiredReadings { get; } = new();

    protected override Task OnGpsReading(GpsReading reading)
    {
        this.FiredReadings.Add(reading);
        return Task.CompletedTask;
    }
}

public class GpsDelegateTests
{
    // Toronto area coordinates - ~111m per 0.001 latitude degree
    static readonly Position Origin = new(43.6532, -79.3832);

    static GpsReading CreateReading(Position position, DateTimeOffset timestamp)
        => new(position, 1, timestamp, 0, 0, 0, 0, 0);

    static Position OffsetMeters(double meters)
    {
        // ~111,000 meters per degree of latitude
        var latOffset = meters / 111_000.0;
        return new Position(Origin.Latitude + latOffset, Origin.Longitude);
    }

    [Fact]
    public async Task FirstReading_AlwaysFires()
    {
        var del = new TestGpsDelegate();
        var reading = CreateReading(Origin, DateTimeOffset.UtcNow);

        await del.OnReading(reading);

        Assert.Single(del.FiredReadings);
    }

    [Fact]
    public async Task NoFilters_AlwaysFires()
    {
        var del = new TestGpsDelegate();
        var now = DateTimeOffset.UtcNow;

        await del.OnReading(CreateReading(Origin, now));
        await del.OnReading(CreateReading(Origin, now.AddSeconds(1)));

        Assert.Equal(2, del.FiredReadings.Count);
    }

    // --- MinimumDistance only ---

    [Fact]
    public async Task MinDistanceOnly_Fires_WhenMet()
    {
        var del = new TestGpsDelegate { MinimumDistance = Distance.FromMeters(100) };
        var now = DateTimeOffset.UtcNow;

        await del.OnReading(CreateReading(Origin, now));
        await del.OnReading(CreateReading(OffsetMeters(150), now.AddSeconds(1)));

        Assert.Equal(2, del.FiredReadings.Count);
    }

    [Fact]
    public async Task MinDistanceOnly_DoesNotFire_WhenNotMet()
    {
        var del = new TestGpsDelegate { MinimumDistance = Distance.FromMeters(100) };
        var now = DateTimeOffset.UtcNow;

        await del.OnReading(CreateReading(Origin, now));
        await del.OnReading(CreateReading(OffsetMeters(50), now.AddSeconds(1)));

        Assert.Single(del.FiredReadings);
    }

    // --- MinimumTime only ---

    [Fact]
    public async Task MinTimeOnly_Fires_WhenMet()
    {
        var del = new TestGpsDelegate { MinimumTime = TimeSpan.FromSeconds(10) };
        var now = DateTimeOffset.UtcNow;

        await del.OnReading(CreateReading(Origin, now));
        await del.OnReading(CreateReading(Origin, now.AddSeconds(15)));

        Assert.Equal(2, del.FiredReadings.Count);
    }

    [Fact]
    public async Task MinTimeOnly_DoesNotFire_WhenNotMet()
    {
        var del = new TestGpsDelegate { MinimumTime = TimeSpan.FromSeconds(10) };
        var now = DateTimeOffset.UtcNow;

        await del.OnReading(CreateReading(Origin, now));
        await del.OnReading(CreateReading(Origin, now.AddSeconds(5)));

        Assert.Single(del.FiredReadings);
    }

    // --- Both Minimums (AND) ---

    [Fact]
    public async Task BothMinimums_Fires_WhenBothMet()
    {
        var del = new TestGpsDelegate
        {
            MinimumDistance = Distance.FromMeters(100),
            MinimumTime = TimeSpan.FromSeconds(10)
        };
        var now = DateTimeOffset.UtcNow;

        await del.OnReading(CreateReading(Origin, now));
        await del.OnReading(CreateReading(OffsetMeters(150), now.AddSeconds(15)));

        Assert.Equal(2, del.FiredReadings.Count);
    }

    [Fact]
    public async Task BothMinimums_DoesNotFire_WhenOnlyDistanceMet()
    {
        var del = new TestGpsDelegate
        {
            MinimumDistance = Distance.FromMeters(100),
            MinimumTime = TimeSpan.FromSeconds(10)
        };
        var now = DateTimeOffset.UtcNow;

        await del.OnReading(CreateReading(Origin, now));
        await del.OnReading(CreateReading(OffsetMeters(150), now.AddSeconds(5)));

        Assert.Single(del.FiredReadings);
    }

    [Fact]
    public async Task BothMinimums_DoesNotFire_WhenOnlyTimeMet()
    {
        var del = new TestGpsDelegate
        {
            MinimumDistance = Distance.FromMeters(100),
            MinimumTime = TimeSpan.FromSeconds(10)
        };
        var now = DateTimeOffset.UtcNow;

        await del.OnReading(CreateReading(Origin, now));
        await del.OnReading(CreateReading(OffsetMeters(50), now.AddSeconds(15)));

        Assert.Single(del.FiredReadings);
    }

    // --- MaximumDistance only ---

    [Fact]
    public async Task MaxDistanceOnly_Fires_WhenCrossed()
    {
        var del = new TestGpsDelegate
        {
            MinimumDistance = Distance.FromMeters(100),
            MaximumDistance = Distance.FromMeters(500)
        };
        var now = DateTimeOffset.UtcNow;

        await del.OnReading(CreateReading(Origin, now));
        await del.OnReading(CreateReading(OffsetMeters(600), now.AddSeconds(1)));

        Assert.Equal(2, del.FiredReadings.Count);
    }

    // --- MaximumTime only ---

    [Fact]
    public async Task MaxTimeOnly_Fires_WhenCrossed()
    {
        var del = new TestGpsDelegate
        {
            MinimumTime = TimeSpan.FromSeconds(10),
            MaximumTime = TimeSpan.FromSeconds(60)
        };
        var now = DateTimeOffset.UtcNow;

        await del.OnReading(CreateReading(Origin, now));
        // time exceeds max even though same position (min time not met scenario irrelevant)
        await del.OnReading(CreateReading(Origin, now.AddSeconds(61)));

        Assert.Equal(2, del.FiredReadings.Count);
    }

    // --- Maximum overrides Minimum ---

    [Fact]
    public async Task MaxDistance_OverridesMinimums_WhenBothMinimumsNotMet()
    {
        var del = new TestGpsDelegate
        {
            MinimumDistance = Distance.FromMeters(100),
            MinimumTime = TimeSpan.FromSeconds(10),
            MaximumDistance = Distance.FromMeters(500)
        };
        var now = DateTimeOffset.UtcNow;

        await del.OnReading(CreateReading(Origin, now));
        // distance > max, but time < min time - should still fire because max overrides
        await del.OnReading(CreateReading(OffsetMeters(600), now.AddSeconds(2)));

        Assert.Equal(2, del.FiredReadings.Count);
    }

    [Fact]
    public async Task MaxTime_OverridesMinimums_WhenBothMinimumsNotMet()
    {
        var del = new TestGpsDelegate
        {
            MinimumDistance = Distance.FromMeters(100),
            MinimumTime = TimeSpan.FromSeconds(10),
            MaximumTime = TimeSpan.FromSeconds(60)
        };
        var now = DateTimeOffset.UtcNow;

        await del.OnReading(CreateReading(Origin, now));
        // time > max, but distance not met - should still fire because max overrides
        await del.OnReading(CreateReading(OffsetMeters(10), now.AddSeconds(61)));

        Assert.Equal(2, del.FiredReadings.Count);
    }

    // --- Both Maximums (OR) ---

    [Fact]
    public async Task BothMaximums_Fires_WhenOnlyDistanceCrossed()
    {
        var del = new TestGpsDelegate
        {
            MaximumDistance = Distance.FromMeters(500),
            MaximumTime = TimeSpan.FromSeconds(60)
        };
        var now = DateTimeOffset.UtcNow;

        await del.OnReading(CreateReading(Origin, now));
        await del.OnReading(CreateReading(OffsetMeters(600), now.AddSeconds(5)));

        Assert.Equal(2, del.FiredReadings.Count);
    }

    [Fact]
    public async Task BothMaximums_Fires_WhenOnlyTimeCrossed()
    {
        var del = new TestGpsDelegate
        {
            MaximumDistance = Distance.FromMeters(500),
            MaximumTime = TimeSpan.FromSeconds(60)
        };
        var now = DateTimeOffset.UtcNow;

        await del.OnReading(CreateReading(Origin, now));
        await del.OnReading(CreateReading(OffsetMeters(10), now.AddSeconds(61)));

        Assert.Equal(2, del.FiredReadings.Count);
    }

    // --- No maximum crossed, minimum filters apply ---

    [Fact]
    public async Task MaxNotCrossed_FallsBackToMinimumFilters()
    {
        var del = new TestGpsDelegate
        {
            MinimumDistance = Distance.FromMeters(100),
            MinimumTime = TimeSpan.FromSeconds(10),
            MaximumDistance = Distance.FromMeters(500),
            MaximumTime = TimeSpan.FromSeconds(60)
        };
        var now = DateTimeOffset.UtcNow;

        await del.OnReading(CreateReading(Origin, now));
        // below max thresholds, and only distance met (not time) - should NOT fire
        await del.OnReading(CreateReading(OffsetMeters(150), now.AddSeconds(5)));

        Assert.Single(del.FiredReadings);
    }

    [Fact]
    public async Task AllFiltersSet_BothMinsMet_BelowMax_Fires()
    {
        var del = new TestGpsDelegate
        {
            MinimumDistance = Distance.FromMeters(100),
            MinimumTime = TimeSpan.FromSeconds(10),
            MaximumDistance = Distance.FromMeters(500),
            MaximumTime = TimeSpan.FromSeconds(60)
        };
        var now = DateTimeOffset.UtcNow;

        await del.OnReading(CreateReading(Origin, now));
        // both mins met, below max - should fire via minimum AND logic
        await del.OnReading(CreateReading(OffsetMeters(150), now.AddSeconds(15)));

        Assert.Equal(2, del.FiredReadings.Count);
    }
}

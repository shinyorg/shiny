using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Shiny.Tests;


public class ScreenInfoTests
{
    static readonly ScreenMetrics Portrait = new(1080, 2400, 2.75, DeviceOrientation.Portrait, 120);
    static readonly ScreenMetrics Landscape = new(2400, 1080, 2.75, DeviceOrientation.Landscape, 120);


    [Fact]
    public void Watch_StartsWithFirstSubscriber_StopsWithLast()
    {
        var screen = new FakeScreenInfo { Current = Portrait };
        EventHandler<ScreenMetrics> metrics = (_, _) => { };
        EventHandler<DeviceOrientation> orientation = (_, _) => { };

        screen.MetricsChanged += metrics;
        screen.OrientationChanged += orientation;
        Assert.Equal(1, screen.Starts);

        screen.MetricsChanged -= metrics;
        Assert.Equal(0, screen.Stops);

        screen.OrientationChanged -= orientation;
        Assert.Equal(1, screen.Stops);
    }


    [Fact]
    public void Rotation_RaisesBothEvents()
    {
        var screen = new FakeScreenInfo { Current = Portrait };
        var metrics = new List<ScreenMetrics>();
        var orientations = new List<DeviceOrientation>();
        screen.MetricsChanged += (_, m) => metrics.Add(m);
        screen.OrientationChanged += (_, o) => orientations.Add(o);

        screen.Current = Landscape;
        screen.Fire();

        Assert.Equal([Landscape], metrics);
        Assert.Equal([DeviceOrientation.Landscape], orientations);
    }


    [Fact]
    public void SameOrientation_RaisesOnlyMetrics()
    {
        var screen = new FakeScreenInfo { Current = Portrait };
        var metrics = 0;
        var orientations = 0;
        screen.MetricsChanged += (_, _) => metrics++;
        screen.OrientationChanged += (_, _) => orientations++;

        screen.Current = Portrait with { RefreshRate = 60 };
        screen.Fire();

        Assert.Equal(1, metrics);
        Assert.Equal(0, orientations);
    }


    [Fact]
    public void UnchangedReading_RaisesNothing()
    {
        var screen = new FakeScreenInfo { Current = Portrait };
        var raised = 0;
        screen.MetricsChanged += (_, _) => raised++;

        screen.Fire();
        screen.Fire();

        Assert.Equal(0, raised);
    }


    [Fact]
    public async Task ResetOrientation_AppliesUnknown()
    {
        var screen = new FakeScreenInfo { Current = Portrait };

        await screen.SetOrientation(DeviceOrientation.Landscape);
        await screen.ResetOrientation();

        Assert.Equal([DeviceOrientation.Landscape, DeviceOrientation.Unknown], screen.Applied);
    }


    [Theory]
    [InlineData(1080, 2400, DeviceOrientation.Portrait)]
    [InlineData(2400, 1080, DeviceOrientation.Landscape)]
    [InlineData(0, 0, DeviceOrientation.Unknown)]
    public void OrientationOf_FollowsTheLongerSide(double width, double height, DeviceOrientation expected)
        => Assert.Equal(expected, ScreenMetrics.OrientationOf(width, height));


    [Fact]
    public void IndependentSize_DividesByDensity()
    {
        Assert.Equal(400, new ScreenMetrics(1100, 2200, 2.75, DeviceOrientation.Portrait, 60).WidthIndependent, 3);
        Assert.Equal(0, ScreenMetrics.Empty.WidthIndependent);
    }


    sealed class FakeScreenInfo : AbstractScreenInfo
    {
        Action? refresh;

        public ScreenMetrics Current { get; set; } = ScreenMetrics.Empty;
        public int Starts { get; private set; }
        public int Stops { get; private set; }
        public List<DeviceOrientation> Applied { get; } = new();

        public void Fire() => this.refresh?.Invoke();

        protected override ScreenMetrics ReadMetrics() => this.Current;

        protected override IDisposable Watch(Action refresh)
        {
            this.Starts++;
            this.refresh = refresh;
            return new ActionDisposable(() =>
            {
                this.Stops++;
                this.refresh = null;
            });
        }

        protected override Task<bool> ApplyOrientation(DeviceOrientation orientation)
        {
            this.Applied.Add(orientation);
            return Task.FromResult(true);
        }
    }
}

using System;
using System.Collections.Generic;
using Shiny.Infrastructure;
using Xunit;

namespace Shiny.Tests;


public class ScreenPostureTests
{
    static readonly ScreenSize Window = new(800, 600);

    static ScreenPosture Book(ScreenRect fold) => new(FoldState.HalfOpen, FoldOrientation.Vertical, fold, true, HingeStatus.PartiallyOpen, null, []);
    static ScreenPosture Tabletop(ScreenRect fold) => new(FoldState.HalfOpen, FoldOrientation.Horizontal, fold, true, HingeStatus.PartiallyOpen, null, []);


    [Fact]
    public void Segments_VerticalFold_SplitsLeftAndRight()
    {
        var segments = Book(new ScreenRect(390, 0, 20, 600)).Segments(Window);

        Assert.NotNull(segments);
        Assert.Equal(new ScreenRect(0, 0, 390, 600), segments.Value.First);
        Assert.Equal(new ScreenRect(410, 0, 390, 600), segments.Value.Second);
    }


    [Fact]
    public void Segments_HorizontalFold_SplitsTopAndBottom()
    {
        var segments = Tabletop(new ScreenRect(0, 290, 800, 20)).Segments(Window);

        Assert.NotNull(segments);
        Assert.Equal(new ScreenRect(0, 0, 800, 290), segments.Value.First);
        Assert.Equal(new ScreenRect(0, 310, 800, 290), segments.Value.Second);
    }


    [Fact]
    public void Segments_ZeroWidthHinge_TouchesInTheMiddle()
    {
        var segments = Book(new ScreenRect(400, 0, 0, 600)).Segments(Window);

        Assert.NotNull(segments);
        Assert.Equal(new ScreenRect(0, 0, 400, 600), segments.Value.First);
        Assert.Equal(new ScreenRect(400, 0, 400, 600), segments.Value.Second);
    }


    [Theory]
    [InlineData(900, 0, 20, 600)]   // right of the window
    [InlineData(-40, 0, 20, 600)]   // left of the window
    [InlineData(0, 0, 20, 600)]     // on the left edge - one side would be empty
    [InlineData(780, 0, 20, 600)]   // on the right edge
    public void Segments_FoldOutsideWindow_IsNull(double x, double y, double width, double height)
        => Assert.Null(Book(new ScreenRect(x, y, width, height)).Segments(Window));


    [Fact]
    public void Segments_FoldOverlappingTheEdge_IsClamped()
    {
        var segments = Book(new ScreenRect(790, 0, 20, 600)).Segments(new ScreenSize(800, 600));
        Assert.Null(segments);

        segments = Tabletop(new ScreenRect(0, 580, 800, 10)).Segments(Window);
        Assert.NotNull(segments);
        Assert.Equal(new ScreenRect(0, 590, 800, 10), segments.Value.Second);
    }


    [Fact]
    public void Segments_NotSeparating_IsNull()
    {
        var posture = Book(new ScreenRect(400, 0, 0, 600)) with { IsSeparating = false };
        Assert.Null(posture.Segments(Window));
    }


    [Fact]
    public void Segments_NoFold_IsNull()
    {
        Assert.Null(ScreenPosture.None.Segments(Window));
        Assert.Null(Book(new ScreenRect(400, 0, 0, 600)).Segments(new ScreenSize(0, 0)));
        Assert.Null((Book(new ScreenRect(400, 0, 0, 600)) with { Orientation = FoldOrientation.None }).Segments(Window));
    }


    [Fact]
    public void TabletopAndBook_FollowStateAndOrientation()
    {
        Assert.True(Tabletop(default).IsTabletop);
        Assert.False(Tabletop(default).IsBook);
        Assert.True(Book(default).IsBook);
        Assert.False((Book(default) with { State = FoldState.Flat }).IsBook);
        Assert.False(ScreenPosture.None.IsTabletop);
    }


    [Fact]
    public void Equality_ComparesOcclusionsByContent()
    {
        var a = Book(new ScreenRect(400, 0, 0, 600)) with { Occlusions = [new ScreenRect(1, 2, 3, 4)] };
        var b = Book(new ScreenRect(400, 0, 0, 600)) with { Occlusions = new List<ScreenRect> { new(1, 2, 3, 4) } };
        var c = Book(new ScreenRect(400, 0, 0, 600)) with { Occlusions = [new ScreenRect(1, 2, 3, 5)] };

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
    }


    [Fact]
    public void Watch_StartsWithFirstSubscriber_StopsWithLast()
    {
        var posture = new FakeScreenPosture();
        EventHandler<ScreenPosture> one = (_, _) => { };
        EventHandler<ScreenPosture> two = (_, _) => { };

        posture.Changed += one;
        posture.Changed += two;
        Assert.Equal(1, posture.Starts);

        posture.Changed -= one;
        Assert.Equal(0, posture.Stops);

        posture.Changed -= two;
        Assert.Equal(1, posture.Stops);
    }


    [Fact]
    public void Change_RaisesOnce_AndDuplicatesAreDropped()
    {
        var posture = new FakeScreenPosture();
        var raised = new List<ScreenPosture>();
        posture.Changed += (_, p) => raised.Add(p);

        posture.Fire();                                     // same as the first reading
        var book = Book(new ScreenRect(400, 0, 0, 600));
        posture.Value = book;
        posture.Fire();
        posture.Value = book with { Occlusions = [] };     // equal by content
        posture.Fire();

        Assert.Equal([book], raised);
    }


    [Fact]
    public void Unsupported_ReportsNone()
    {
        var posture = new FakeScreenPosture { Supported = false, Value = Book(new ScreenRect(400, 0, 0, 600)) };
        Assert.Equal(ScreenPosture.None, posture.Current);
        Assert.Equal(ScreenPosture.None, new NetScreenPosture().Current);
        Assert.False(new NetScreenPosture().IsSupported);
    }


    [Fact]
    public void AfterLastUnsubscribe_NativeCallbacksRaiseNothing()
    {
        var posture = new FakeScreenPosture();
        var raised = 0;
        EventHandler<ScreenPosture> handler = (_, _) => raised++;
        posture.Changed += handler;
        var refresh = posture.Refresher!;
        posture.Changed -= handler;

        posture.Value = Book(new ScreenRect(400, 0, 0, 600));
        refresh();

        Assert.Equal(0, raised);
    }


    [Fact]
    public void Browser_SideBySideSegments_AreAVerticalFold()
    {
        var posture = BrowserScreenPosture.ToPosture("folded", [0, 0, 400, 600, 420, 0, 380, 600]);

        Assert.Equal(FoldState.HalfOpen, posture.State);
        Assert.Equal(FoldOrientation.Vertical, posture.Orientation);
        Assert.Equal(new ScreenRect(400, 0, 20, 600), posture.Fold);
        Assert.True(posture.IsSeparating);
        Assert.Equal(HingeStatus.PartiallyOpen, posture.Hinge);
    }


    [Fact]
    public void Browser_StackedSegments_AreAHorizontalFold()
    {
        var posture = BrowserScreenPosture.ToPosture("continuous", [0, 0, 800, 300, 0, 300, 800, 300]);

        Assert.Equal(FoldState.Flat, posture.State);
        Assert.Equal(FoldOrientation.Horizontal, posture.Orientation);
        Assert.Equal(new ScreenRect(0, 300, 800, 0), posture.Fold);
    }


    [Fact]
    public void Browser_FoldedWithoutSegments_IsHalfOpenWithNoFold()
    {
        var posture = BrowserScreenPosture.ToPosture("folded", []);
        Assert.Equal(FoldState.HalfOpen, posture.State);
        Assert.Null(posture.Fold);
        Assert.False(posture.IsSeparating);

        Assert.Equal(ScreenPosture.None, BrowserScreenPosture.ToPosture("continuous", null));
    }


    sealed class FakeScreenPosture : AbstractScreenPosture
    {
        public bool Supported { get; set; } = true;
        public ScreenPosture Value { get; set; } = ScreenPosture.None;
        public int Starts { get; private set; }
        public int Stops { get; private set; }
        public Action? Refresher { get; private set; }

        public override bool IsSupported => this.Supported;
        public void Fire() => this.Refresher?.Invoke();

        protected override ScreenPosture ReadPosture() => this.Value;

        protected override IDisposable Watch(Action refresh)
        {
            this.Starts++;
            this.Refresher = refresh;
            return new ActionDisposable(() =>
            {
                this.Stops++;
                this.Refresher = null;
            });
        }
    }
}

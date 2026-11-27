using Shiny.ScreenRecorder.Infrastructure;
using Xunit;

namespace Shiny.ScreenRecorder.Tests;


/// <summary>
/// The pause arithmetic every synthesised-pause backend shares. Getting it wrong leaves the paused
/// span in the file as a frozen stretch, which only shows up on playback.
/// </summary>
public class PauseTimelineTests
{
    // a 30fps capture on a raw clock that, like every platform's, does not start at zero
    const long Start = 5_000_000;
    const long Frame = 33_333;


    static long Write(PauseTimeline timeline, long raw)
    {
        Assert.True(timeline.TryMap(raw, out var mapped));
        timeline.Commit(raw, mapped);
        return mapped;
    }


    [Fact]
    public void NoPausePassesTimestampsThrough()
    {
        var timeline = new PauseTimeline();

        Assert.Equal(Start, Write(timeline, Start));
        Assert.Equal(Start + Frame, Write(timeline, Start + Frame));
    }


    [Fact]
    public void PausedFramesAreDropped()
    {
        var timeline = new PauseTimeline();
        Write(timeline, Start);
        timeline.Pause();

        Assert.False(timeline.TryMap(Start + Frame, out _));
    }


    [Fact]
    public void PausedSpanIsRemovedFromTheTimeline()
    {
        var timeline = new PauseTimeline();
        Write(timeline, Start);
        Write(timeline, Start + Frame);

        timeline.Pause();
        for (var i = 2; i < 300; i++)
            Assert.False(timeline.TryMap(Start + (i * Frame), out _));
        timeline.Resume();

        // ten seconds of pause; the first frame after resume lands one frame after the last
        // written one, not ten seconds later
        var resumed = Write(timeline, Start + (300 * Frame));
        Assert.Equal(Start + (2 * Frame), resumed);

        var next = Write(timeline, Start + (301 * Frame));
        Assert.Equal(Start + (3 * Frame), next);
    }


    [Fact]
    public void PauseWithNoFramesStillCloses()
    {
        // a static screen produces no frames at all while paused
        var timeline = new PauseTimeline();
        Write(timeline, Start);

        timeline.Pause();
        timeline.Resume();

        var resumed = Write(timeline, Start + (600 * Frame));
        Assert.True(resumed > Start);
        Assert.True(resumed - Start < Frame);
    }


    [Fact]
    public void FirstDroppedFrameAnchorsTheGap()
    {
        var timeline = new PauseTimeline();
        Write(timeline, Start);

        timeline.Pause();
        Assert.False(timeline.TryMap(Start + Frame, out _));
        timeline.Resume();

        var resumed = Write(timeline, Start + (100 * Frame));
        Assert.Equal(Start + Frame, resumed);
    }


    [Fact]
    public void BackToBackPausesWithNoFrameBetweenAreOneGap()
    {
        var timeline = new PauseTimeline();
        Write(timeline, Start);

        timeline.Pause();
        Assert.False(timeline.TryMap(Start + Frame, out _));
        timeline.Resume();
        timeline.Pause();
        Assert.False(timeline.TryMap(Start + (50 * Frame), out _));
        timeline.Resume();

        var resumed = Write(timeline, Start + (100 * Frame));
        Assert.Equal(Start + Frame, resumed);
    }


    [Fact]
    public void DurationIsMeasuredFromTheFirstTimestamp()
    {
        var timeline = new PauseTimeline();
        Write(timeline, Start);
        Write(timeline, Start + (30 * Frame));

        timeline.Pause();
        Assert.False(timeline.TryMap(Start + (31 * Frame), out _));
        timeline.Resume();
        Write(timeline, Start + (300 * Frame));

        // 30 frames before the pause, one after it - not the raw clock's value
        Assert.Equal(31 * Frame, timeline.Duration);
    }


    [Fact]
    public void TimestampsKeepMovingForwardAfterResume()
    {
        var timeline = new PauseTimeline();
        var last = Write(timeline, Start);

        for (var round = 1; round <= 5; round++)
        {
            timeline.Pause();
            timeline.Resume();

            var mapped = Write(timeline, Start + (round * 1000 * Frame));
            Assert.True(mapped > last);
            last = mapped;
        }
    }
}

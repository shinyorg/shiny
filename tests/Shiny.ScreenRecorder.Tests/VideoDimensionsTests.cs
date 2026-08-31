using Shiny.ScreenRecorder.Infrastructure;
using Xunit;

namespace Shiny.ScreenRecorder.Tests;


/// <summary>
/// Sizing is shared by every backend, and getting it wrong fails in ways that are hard to see.
/// </summary>
/// <remarks>
/// An odd width is rejected outright by H.264 on some encoders and silently produces a sheared
/// image on others; a bitrate derived the obvious way from a 4K display asks for hundreds of
/// megabits. Both are pinned here because neither shows up until a real recording is played back.
/// </remarks>
public class VideoDimensionsTests
{
    [Fact]
    public void NativeSizeIsKeptWhenNothingIsAsked()
    {
        var result = VideoDimensions.From(new ScreenRecordingRequest(), 1920, 1080);

        Assert.Equal(1920, result.Width);
        Assert.Equal(1080, result.Height);
        Assert.Equal(VideoDimensions.DefaultFrameRate, result.FrameRate);
    }


    [Fact]
    public void MaxWidthScalesHeightProportionally()
    {
        var result = VideoDimensions.From(new ScreenRecordingRequest { MaxWidth = 1280 }, 2560, 1440);

        Assert.Equal(1280, result.Width);
        Assert.Equal(720, result.Height);
    }


    [Fact]
    public void MaxWidthLargerThanNativeDoesNotUpscale()
    {
        var result = VideoDimensions.From(new ScreenRecordingRequest { MaxWidth = 4096 }, 1280, 720);

        Assert.Equal(1280, result.Width);
        Assert.Equal(720, result.Height);
    }


    [Theory]
    [InlineData(1179, 2556)]   // iPhone 15 - both sides odd
    [InlineData(1125, 2436)]
    [InlineData(1440, 3201)]
    public void DimensionsAreAlwaysEven(int width, int height)
    {
        var result = VideoDimensions.From(new ScreenRecordingRequest(), width, height);

        Assert.Equal(0, result.Width % 2);
        Assert.Equal(0, result.Height % 2);
    }


    [Fact]
    public void ScaledOddDimensionsAreStillEven()
    {
        // 1179x2556 scaled to 720 wide lands on a fractional height; rounding it must not leave odd
        var result = VideoDimensions.From(new ScreenRecordingRequest { MaxWidth = 721 }, 1179, 2556);

        Assert.Equal(0, result.Width % 2);
        Assert.Equal(0, result.Height % 2);
    }


    [Fact]
    public void HeightIsDerivedFromTheUnroundedRatio()
    {
        // rounding the width to even first and then scaling would give 1170/1179 of the height;
        // the aspect error compounds on tall screens, which is why the ratio uses the raw max
        var result = VideoDimensions.From(new ScreenRecordingRequest { MaxWidth = 590 }, 1179, 2556);

        var expected = (int)Math.Round(2556 * (590 / 1179d));
        Assert.Equal(expected - (expected % 2), result.Height);
    }


    [Fact]
    public void ExplicitBitrateIsHonoured()
    {
        var result = VideoDimensions.From(new ScreenRecordingRequest { VideoBitrate = 3_000_000 }, 1920, 1080);

        Assert.Equal(3_000_000, result.Bitrate);
    }


    [Fact]
    public void EstimatedBitrateStaysReadableOnSmallScreens()
    {
        // 320x240 at 30fps estimates well under a megabit, which turns text into mush - the floor
        // is the point of the clamp
        var result = VideoDimensions.From(new ScreenRecordingRequest(), 320, 240);

        Assert.True(result.Bitrate >= 1_500_000);
    }


    [Fact]
    public void EstimatedBitrateIsCappedOnLargeScreens()
    {
        var result = VideoDimensions.From(new ScreenRecordingRequest { FrameRate = 60 }, 3840, 2160);

        Assert.True(result.Bitrate <= 40_000_000);
    }


    [Fact]
    public void DegenerateSizeStillProducesAnEncodableFrame()
    {
        var result = VideoDimensions.From(new ScreenRecordingRequest(), 1, 1);

        Assert.Equal(2, result.Width);
        Assert.Equal(2, result.Height);
    }


    [Theory]
    [InlineData(0, 1080)]
    [InlineData(1920, 0)]
    [InlineData(-1, -1)]
    public void NonsensicalNativeSizeThrows(int width, int height)
        => Assert.Throws<ScreenRecorderException>(() => VideoDimensions.From(new ScreenRecordingRequest(), width, height));
}

using Shiny.ScreenRecorder.Encoders;
using Shiny.ScreenRecorder.Infrastructure;
using Xunit;

namespace Shiny.ScreenRecorder.Tests;


/// <summary>
/// The Linux command lines, which are the only part of that backend testable without a compositor.
/// </summary>
/// <remarks>
/// They are also where the failures live. A dropped <c>-e</c> leaves gst-launch killing the
/// pipeline on SIGINT without an end-of-stream, so <c>mp4mux</c> never writes the moov atom and the
/// file will not open. A bitrate handed to <c>x264enc</c> in bits rather than kilobits produces a
/// file a thousand times too large. Neither shows up until playback.
/// </remarks>
public class EncoderCommandTests
{
    static readonly VideoDimensions Dimensions = new(1280, 720, 4_000_000, 30);


    [Fact]
    public void GStreamerSendsEndOfStreamOnInterrupt()
    {
        var command = EncoderCommandBuilder.GStreamer(42, Dimensions, "/tmp/out.mp4", null, false);

        Assert.Equal("gst-launch-1.0", command.FileName);
        Assert.Equal("-e", command.Arguments[0]);
    }


    [Fact]
    public void GStreamerReadsTheNodeThePortalGaveIt()
    {
        var command = EncoderCommandBuilder.GStreamer(1234, Dimensions, "/tmp/out.mp4", null, false);

        Assert.Contains("pipewiresrc", command.Arguments);
        Assert.Contains("path=1234", command.Arguments);
    }


    [Fact]
    public void GStreamerBitrateIsInKilobits()
    {
        var command = EncoderCommandBuilder.GStreamer(1, Dimensions, "/tmp/out.mp4", null, false);

        Assert.Contains("bitrate=4000", command.Arguments);
        Assert.DoesNotContain("bitrate=4000000", command.Arguments);
    }


    [Fact]
    public void GStreamerConstrainsSizeAndFrameRate()
    {
        var command = EncoderCommandBuilder.GStreamer(1, Dimensions, "/tmp/out.mp4", null, false);

        Assert.Contains("video/x-raw,width=1280,height=720,framerate=30/1", command.Arguments);
    }


    [Fact]
    public void GStreamerKeyframeIntervalIsTwoSeconds()
    {
        var command = EncoderCommandBuilder.GStreamer(1, Dimensions with { FrameRate = 60 }, "/tmp/out.mp4", null, false);

        Assert.Contains("key-int-max=120", command.Arguments);
    }


    [Fact]
    public void GStreamerOutputPathIsNotQuotedOrEscaped()
    {
        // the arguments go through ProcessStartInfo.ArgumentList, which passes each one untouched -
        // adding quotes here would put them in the filename
        var command = EncoderCommandBuilder.GStreamer(1, Dimensions, "/tmp/my recording.mp4", null, false);

        Assert.Contains("location=/tmp/my recording.mp4", command.Arguments);
    }


    [Fact]
    public void GStreamerHasNoAudioBranchWhenNoneIsAsked()
    {
        var command = EncoderCommandBuilder.GStreamer(1, Dimensions, "/tmp/out.mp4", null, false);

        Assert.DoesNotContain("pulsesrc", command.Arguments);
    }


    [Fact]
    public void GStreamerSystemAudioPointsAtTheMonitorSource()
    {
        var command = EncoderCommandBuilder.GStreamer(1, Dimensions, "/tmp/out.mp4", "alsa_output.pci-0000_00_1f.3.analog-stereo.monitor", false);

        Assert.Contains("pulsesrc", command.Arguments);
        Assert.Contains("device=alsa_output.pci-0000_00_1f.3.analog-stereo.monitor", command.Arguments);
        Assert.Contains("mux.", command.Arguments);
    }


    [Fact]
    public void GStreamerMicrophoneUsesTheDefaultSource()
    {
        var command = EncoderCommandBuilder.GStreamer(1, Dimensions, "/tmp/out.mp4", null, true);

        Assert.Contains("pulsesrc", command.Arguments);
        Assert.DoesNotContain(command.Arguments, a => a.StartsWith("device=", StringComparison.Ordinal));
    }


    [Fact]
    public void GStreamerMixesWhenBothAudioSourcesAreWanted()
    {
        // two pulsesrc branches straight into the muxer would produce two audio tracks, and most
        // players only ever use the first - so they have to be summed first
        var command = EncoderCommandBuilder.GStreamer(1, Dimensions, "/tmp/out.mp4", "sink.monitor", true);

        Assert.Contains("audiomixer", command.Arguments);
        Assert.Contains("name=amix", command.Arguments);
        Assert.Equal(2, command.Arguments.Count(a => a == "pulsesrc"));
        Assert.Equal(1, command.Arguments.Count(a => a == "avenc_aac"));
    }


    [Fact]
    public void FfmpegGrabsTheNamedDisplay()
    {
        var command = EncoderCommandBuilder.FfmpegX11(":1", Dimensions, "/tmp/out.mp4", true, false);

        Assert.Equal("ffmpeg", command.FileName);
        Assert.Contains("x11grab", command.Arguments);

        var displayIndex = command.Arguments.ToList().IndexOf("-i");
        Assert.Equal(":1", command.Arguments[displayIndex + 1]);
    }


    [Fact]
    public void FfmpegOverwritesWithoutPrompting()
    {
        // without -y ffmpeg blocks on a "File exists. Overwrite?" prompt that nothing will answer
        var command = EncoderCommandBuilder.FfmpegX11(":0", Dimensions, "/tmp/out.mp4", true, false);

        Assert.Contains("-y", command.Arguments);
    }


    [Theory]
    [InlineData(true, "1")]
    [InlineData(false, "0")]
    public void FfmpegDrawsTheCursorOnlyWhenAsked(bool showCursor, string expected)
    {
        var command = EncoderCommandBuilder.FfmpegX11(":0", Dimensions, "/tmp/out.mp4", showCursor, false);

        var index = command.Arguments.ToList().IndexOf("-draw_mouse");
        Assert.Equal(expected, command.Arguments[index + 1]);
    }


    [Fact]
    public void FfmpegScalesToTheRequestedSize()
    {
        var command = EncoderCommandBuilder.FfmpegX11(":0", Dimensions, "/tmp/out.mp4", true, false);

        Assert.Contains("scale=1280:720", command.Arguments);
    }


    [Fact]
    public void FfmpegProducesAWidelyPlayablePixelFormat()
    {
        // x11grab is bgr0; leaving it produces a file only FFmpeg itself will open
        var command = EncoderCommandBuilder.FfmpegX11(":0", Dimensions, "/tmp/out.mp4", true, false);

        Assert.Contains("yuv420p", command.Arguments);
    }


    [Fact]
    public void FfmpegAddsAudioOnlyWhenAsked()
    {
        var without = EncoderCommandBuilder.FfmpegX11(":0", Dimensions, "/tmp/out.mp4", true, false);
        var with = EncoderCommandBuilder.FfmpegX11(":0", Dimensions, "/tmp/out.mp4", true, true);

        Assert.DoesNotContain("pulse", without.Arguments);
        Assert.Contains("pulse", with.Arguments);
        Assert.Contains("aac", with.Arguments);
    }


    [Fact]
    public void OutputPathIsTheLastFfmpegArgument()
    {
        var command = EncoderCommandBuilder.FfmpegX11(":0", Dimensions, "/tmp/out.mp4", true, true);

        Assert.Equal("/tmp/out.mp4", command.Arguments[^1]);
    }
}

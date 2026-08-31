using Xunit;

namespace Shiny.ScreenRecorder.Tests;


/// <summary>
/// Validation is what stops a platform silently ignoring a setting.
/// </summary>
/// <remarks>
/// Every one of these settings is quietly dropped by at least one backend if it reaches native
/// code - ReplayKit has no frame rate, Windows has no audio, the browser always draws the cursor.
/// A recording that came out wrong is far worse than one that refused to start, so the request is
/// checked against the capability flags before any native call happens, and these pin that.
/// </remarks>
public class ScreenRecordingRequestTests
{
    const ScreenRecorderCapabilities Minimal = ScreenRecorderCapabilities.Recording;
    const string Reason = "the test platform";


    [Fact]
    public void PlainRequestPassesOnAMinimalPlatform()
    {
        var exception = Record.Exception(() => new ScreenRecordingRequest().AssertValid(Minimal, Reason));

        Assert.Null(exception);
    }


    [Fact]
    public void RecordingCapabilityIsRequired()
    {
        var ex = Assert.Throws<ScreenRecorderNotSupportedException>(
            () => new ScreenRecordingRequest().AssertValid(ScreenRecorderCapabilities.None, Reason)
        );

        Assert.Contains("Recording", ex.Message);
        Assert.Contains(Reason, ex.Message);
    }


    [Fact]
    public void MicrophoneNeedsTheMicrophoneCapability()
    {
        var request = new ScreenRecordingRequest { IncludeMicrophone = true };

        Assert.Throws<ScreenRecorderNotSupportedException>(() => request.AssertValid(Minimal, Reason));
        request.AssertValid(Minimal | ScreenRecorderCapabilities.Microphone, Reason);
    }


    [Fact]
    public void SystemAudioNeedsTheSystemAudioCapability()
    {
        var request = new ScreenRecordingRequest { IncludeSystemAudio = true };

        Assert.Throws<ScreenRecorderNotSupportedException>(() => request.AssertValid(Minimal, Reason));
        request.AssertValid(Minimal | ScreenRecorderCapabilities.SystemAudio, Reason);
    }


    [Fact]
    public void HidingTheCursorNeedsTheCursorCapability()
    {
        // ShowCursor defaults to true, so only turning it *off* is a request the platform must
        // actually be able to honour
        new ScreenRecordingRequest { ShowCursor = true }.AssertValid(Minimal, Reason);

        Assert.Throws<ScreenRecorderNotSupportedException>(
            () => new ScreenRecordingRequest { ShowCursor = false }.AssertValid(Minimal, Reason)
        );
    }


    [Fact]
    public void FrameRateNeedsTheFrameRateCapability()
    {
        var request = new ScreenRecordingRequest { FrameRate = 60 };

        Assert.Throws<ScreenRecorderNotSupportedException>(() => request.AssertValid(Minimal, Reason));
        request.AssertValid(Minimal | ScreenRecorderCapabilities.FrameRateControl, Reason);
    }


    [Fact]
    public void BitrateNeedsTheBitrateCapability()
    {
        var request = new ScreenRecordingRequest { VideoBitrate = 4_000_000 };

        Assert.Throws<ScreenRecorderNotSupportedException>(() => request.AssertValid(Minimal, Reason));
        request.AssertValid(Minimal | ScreenRecorderCapabilities.BitrateControl, Reason);
    }


    [Fact]
    public void MaxWidthNeedsTheDownscalingCapability()
    {
        var request = new ScreenRecordingRequest { MaxWidth = 1280 };

        Assert.Throws<ScreenRecorderNotSupportedException>(() => request.AssertValid(Minimal, Reason));
        request.AssertValid(Minimal | ScreenRecorderCapabilities.Downscaling, Reason);
    }


    [Fact]
    public void DisplayTargetNeedsDisplaySelection()
    {
        var request = new ScreenRecordingRequest
        {
            Target = new CaptureTarget { Id = "Display:1", Kind = CaptureTargetKind.Display, Name = "Main" }
        };

        Assert.Throws<ScreenRecorderNotSupportedException>(() => request.AssertValid(Minimal, Reason));
        request.AssertValid(Minimal | ScreenRecorderCapabilities.DisplaySelection, Reason);
    }


    [Fact]
    public void WindowTargetNeedsWindowSelectionNotDisplaySelection()
    {
        var request = new ScreenRecordingRequest
        {
            Target = new CaptureTarget { Id = "Window:9", Kind = CaptureTargetKind.Window, Name = "Editor" }
        };

        // a platform that lists displays but not windows must still refuse a window
        Assert.Throws<ScreenRecorderNotSupportedException>(
            () => request.AssertValid(Minimal | ScreenRecorderCapabilities.DisplaySelection, Reason)
        );

        request.AssertValid(Minimal | ScreenRecorderCapabilities.WindowSelection, Reason);
    }


    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(241)]
    public void NonsensicalFrameRateThrows(int frameRate)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => new ScreenRecordingRequest { FrameRate = frameRate }
                .AssertValid(Minimal | ScreenRecorderCapabilities.FrameRateControl, Reason)
        );


    [Fact]
    public void NonPositiveBitrateThrows()
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => new ScreenRecordingRequest { VideoBitrate = 0 }
                .AssertValid(Minimal | ScreenRecorderCapabilities.BitrateControl, Reason)
        );


    [Fact]
    public void NonPositiveMaxWidthThrows()
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => new ScreenRecordingRequest { MaxWidth = -10 }
                .AssertValid(Minimal | ScreenRecorderCapabilities.Downscaling, Reason)
        );


    [Fact]
    public void NonPositiveMaxDurationThrows()
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => new ScreenRecordingRequest { MaxDuration = TimeSpan.Zero }.AssertValid(Minimal, Reason)
        );


    [Fact]
    public void ArgumentValidationRunsBeforeCapabilityChecks()
    {
        // a nonsense frame rate is a programming error either way; reporting "not supported" for it
        // on a platform that has no frame rate control would send the caller down the wrong path
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ScreenRecordingRequest { FrameRate = -5 }.AssertValid(Minimal, Reason)
        );
    }
}

using AVFoundation;
using CoreFoundation;
using CoreMedia;
using Foundation;
using Microsoft.Extensions.Logging;
using ScreenCaptureKit;
using Shiny.ScreenRecorder.Infrastructure;

namespace Shiny.ScreenRecorder;


/// <summary>
/// A live ScreenCaptureKit session, writing either through <c>SCRecordingOutput</c> (macOS 15+) or
/// through <see cref="AssetWriterSink"/>.
/// </summary>
/// <remarks>
/// The sample callbacks arrive on a dedicated serial dispatch queue rather than the main one -
/// ScreenCaptureKit drops frames when its output handler is slow, and the main queue on a UI app
/// is exactly where slow work happens.
/// </remarks>
class MacOSScreenRecording : AbstractScreenRecording
{
    readonly SCContentFilter filter;
    readonly SCStreamConfiguration config;
    readonly VideoDimensions dimensions;
    readonly string outputPath;
    readonly bool useRecordingOutput;
    readonly DispatchQueue sampleQueue = new("shiny.screenrecorder.samples");

    SCStream? stream;
    SCRecordingOutput? recordingOutput;
    AssetWriterSink? sink;
    StreamCallbacks? callbacks;


    public MacOSScreenRecording(
        ScreenRecordingRequest request,
        ScreenRecorderCapabilities capabilities,
        string platformReason,
        SCContentFilter filter,
        SCStreamConfiguration config,
        VideoDimensions dimensions,
        string outputPath,
        bool useRecordingOutput,
        ILogger logger
    ) : base(request, capabilities, platformReason, logger)
    {
        this.filter = filter;
        this.config = config;
        this.dimensions = dimensions;
        this.outputPath = outputPath;
        this.useRecordingOutput = useRecordingOutput;
    }


    protected override string? OutputFilePath => this.outputPath;


    public async Task Start(CancellationToken ct)
    {
        this.callbacks = new StreamCallbacks(this);
        this.stream = new SCStream(this.filter, this.config, this.callbacks);

        if (this.useRecordingOutput)
        {
            if (File.Exists(this.outputPath))
                File.Delete(this.outputPath);

            var outputConfig = new SCRecordingOutputConfiguration
            {
                OutputUrl = NSUrl.FromFilename(this.outputPath),
                OutputFileType = AVFileTypes.Mpeg4,
                VideoCodecType = AVVideoCodecType.H264
            };

            this.recordingOutput = new SCRecordingOutput(outputConfig, this.callbacks);

            if (!this.stream.AddRecordingOutput(this.recordingOutput, out var recordingError))
                throw new ScreenRecorderException($"ScreenCaptureKit refused the recording output - {recordingError?.LocalizedDescription ?? "unknown error"}");
        }
        else
        {
            this.sink = new AssetWriterSink(
                this.outputPath,
                this.dimensions,
                this.Request.IncludeSystemAudio,
                false,
                this.Logger
            );

            if (!this.stream.AddStreamOutput(this.callbacks, SCStreamOutputType.Screen, this.sampleQueue, out var screenError))
                throw new ScreenRecorderException($"ScreenCaptureKit refused the screen output - {screenError?.LocalizedDescription ?? "unknown error"}");

            if (this.Request.IncludeSystemAudio && !this.stream.AddStreamOutput(this.callbacks, SCStreamOutputType.Audio, this.sampleQueue, out var audioError))
                throw new ScreenRecorderException($"ScreenCaptureKit refused the audio output - {audioError?.LocalizedDescription ?? "unknown error"}");
        }

        await this.StartCapture(ct).ConfigureAwait(false);
        this.BeginClock();
    }


    Task StartCapture(CancellationToken ct)
    {
        var tcs = new TaskCompletionSource();
        using var registration = ct.Register(() => tcs.TrySetCanceled(ct));

        this.stream!.StartCapture(error =>
        {
            if (error == null)
            {
                tcs.TrySetResult();
                return;
            }

            // -3801 is SCStreamErrorCode.UserDeclined; every other failure here is a genuine fault
            tcs.TrySetException(
                error.Code == (long)SCStreamErrorCode.UserDeclined
                    ? new ScreenRecorderPermissionException("The user declined the screen capture")
                    : new ScreenRecorderException($"ScreenCaptureKit could not start - {error.LocalizedDescription}")
            );
        });

        return tcs.Task;
    }


    Task StopCapture()
    {
        if (this.stream == null)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource();
        this.stream.StopCapture(error =>
        {
            // a stop that reports an error still stopped; the file is what matters and it is
            // checked separately, so this is logged rather than thrown
            if (error != null)
                this.Logger.StopReportedError(error.LocalizedDescription);

            tcs.TrySetResult();
        });

        return tcs.Task;
    }


    protected override Task OnPause(CancellationToken ct)
    {
        this.sink?.Pause();
        return Task.CompletedTask;
    }


    protected override Task OnResume(CancellationToken ct)
    {
        this.sink?.Resume();
        return Task.CompletedTask;
    }


    protected override async Task<ScreenRecordingResult> OnStop(CancellationToken ct)
    {
        await this.StopCapture().ConfigureAwait(false);

        // read after stopping and before teardown: SCRecordingOutput keeps counting until the
        // stream ends, so reading it first reports a duration short by however long the stop took
        var recordedDuration = this.recordingOutput?.RecordedDuration;

        if (this.sink != null)
            await this.sink.Finish().ConfigureAwait(false);

        this.Teardown();

        if (!File.Exists(this.outputPath))
            throw new ScreenRecorderException("ScreenCaptureKit stopped without producing a file");

        var info = new FileInfo(this.outputPath);

        // SCRecordingOutput reports the duration it actually wrote, which is more accurate than the
        // session stopwatch; the AVAssetWriter path takes it from the last frame it appended
        var duration = recordedDuration is { IsNumeric: true } d
            ? TimeSpan.FromSeconds(d.Seconds)
            : this.sink?.Duration ?? this.Elapsed;

        return new ScreenRecordingResult
        {
            FilePath = this.outputPath,
            Duration = duration == TimeSpan.Zero ? this.Elapsed : duration,
            ByteSize = info.Length,
            Width = this.dimensions.Width,
            Height = this.dimensions.Height,
            MimeType = "video/mp4"
        };
    }


    protected override async Task OnCancel(CancellationToken ct)
    {
        await this.StopCapture().ConfigureAwait(false);
        this.sink?.Abort();
        this.Teardown();
    }


    void Teardown()
    {
        this.sink?.Dispose();
        this.sink = null;
        this.recordingOutput?.Dispose();
        this.recordingOutput = null;
        this.stream?.Dispose();
        this.stream = null;
        this.callbacks?.Dispose();
        this.callbacks = null;
    }


    void OnSample(CMSampleBuffer buffer, SCStreamOutputType type)
    {
        if (this.sink == null || this.IsFinished)
            return;

        var track = type switch
        {
            SCStreamOutputType.Screen => AssetWriterTrack.Video,
            SCStreamOutputType.Audio => AssetWriterTrack.SystemAudio,
            SCStreamOutputType.Microphone => AssetWriterTrack.Microphone,
            _ => (AssetWriterTrack?)null
        };

        if (track == null)
            return;

        try
        {
            this.sink.Append(buffer, track.Value);
        }
        catch (Exception ex)
        {
            this.OnPlatformStopped(ScreenRecordingFaultReason.EncoderFailed, ex);
        }
    }


    /// <summary>
    /// One NSObject wearing all three ScreenCaptureKit protocols.
    /// </summary>
    /// <remarks>
    /// They are separate protocols but always have the same lifetime here, and keeping them on one
    /// object means one strong reference to hold rather than three - which matters, because
    /// ScreenCaptureKit holds its delegates weakly and a collected one silently stops delivering.
    /// </remarks>
    sealed class StreamCallbacks(MacOSScreenRecording owner) : NSObject, ISCStreamOutput, ISCStreamDelegate, ISCRecordingOutputDelegate
    {
        [Export("stream:didOutputSampleBuffer:ofType:")]
        public void DidOutputSampleBuffer(SCStream stream, CMSampleBuffer sampleBuffer, SCStreamOutputType type)
            => owner.OnSample(sampleBuffer, type);


        [Export("stream:didStopWithError:")]
        public void DidStop(SCStream stream, NSError error)
            => owner.OnPlatformStopped(
                error.Code == (long)SCStreamErrorCode.UserStopped
                    ? ScreenRecordingFaultReason.RevokedByUser
                    : ScreenRecordingFaultReason.TargetLost,
                new NSErrorException(error)
            );


        [Export("userDidStopStream:")]
        public void UserDidStop(SCStream stream)
            => owner.OnPlatformStopped(ScreenRecordingFaultReason.RevokedByUser, null);


        [Export("recordingOutput:didFailWithError:")]
        public void DidFail(SCRecordingOutput recordingOutput, NSError error)
            => owner.OnPlatformStopped(ScreenRecordingFaultReason.EncoderFailed, new NSErrorException(error));


        [Export("recordingOutputDidFinishRecording:")]
        public void DidFinishRecording(SCRecordingOutput recordingOutput) { }


        [Export("recordingOutputDidStartRecording:")]
        public void DidStartRecording(SCRecordingOutput recordingOutput) { }
    }
}

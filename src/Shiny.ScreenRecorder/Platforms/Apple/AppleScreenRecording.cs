using CoreMedia;
using CoreVideo;
using Foundation;
using Microsoft.Extensions.Logging;
using ReplayKit;
using Shiny.ScreenRecorder.Infrastructure;

namespace Shiny.ScreenRecorder;


/// <summary>
/// A live ReplayKit capture, writing through <see cref="AssetWriterSink"/>.
/// </summary>
/// <remarks>
/// <para>The writer is created lazily, on the first video buffer, rather than up front. ReplayKit
/// does not tell you what size it is going to deliver, and the honest answer is not
/// <c>UIScreen.MainScreen</c> - it varies with the device, the interface orientation at the moment
/// capture started, and on Mac Catalyst the window size. Reading it off the first
/// <c>CVPixelBuffer</c> is the only source that is actually correct.</para>
/// <para>ReplayKit always delivers app audio once capture is running, whether or not it was asked
/// for. <see cref="ScreenRecordingRequest.IncludeSystemAudio"/> therefore decides whether those
/// buffers are written, not whether they arrive.</para>
/// </remarks>
class AppleScreenRecording : AbstractScreenRecording
{
    readonly object sinkGate = new();
    readonly string outputPath;
    readonly RecorderDelegate recorderDelegate;

    AssetWriterSink? sink;
    VideoDimensions dimensions;


    public AppleScreenRecording(
        ScreenRecordingRequest request,
        ScreenRecorderCapabilities capabilities,
        string platformReason,
        string outputPath,
        ILogger logger
    ) : base(request, capabilities, platformReason, logger)
    {
        this.outputPath = outputPath;
        this.recorderDelegate = new RecorderDelegate(this);
    }


    protected override string? OutputFilePath => this.outputPath;


    public async Task Start(CancellationToken ct)
    {
        var recorder = RPScreenRecorder.SharedRecorder;
        recorder.Delegate = this.recorderDelegate;

        // must be set before capture starts; toggling it afterwards is ignored
        recorder.MicrophoneEnabled = this.Request.IncludeMicrophone;

        var tcs = new TaskCompletionSource();
        using var registration = ct.Register(() => tcs.TrySetCanceled(ct));

        recorder.StartCapture(
            this.OnSample,
            error =>
            {
                if (error == null)
                {
                    tcs.TrySetResult();
                    return;
                }

                tcs.TrySetException(Translate(error));
            }
        );

        await tcs.Task.ConfigureAwait(false);
        this.BeginClock();
    }


    void OnSample(CMSampleBuffer buffer, RPSampleBufferType type, NSError? error)
    {
        if (error != null)
        {
            this.OnPlatformStopped(ScreenRecordingFaultReason.EncoderFailed, new NSErrorException(error));
            return;
        }

        if (this.IsFinished || buffer == null)
            return;

        try
        {
            var track = type switch
            {
                RPSampleBufferType.Video => AssetWriterTrack.Video,
                RPSampleBufferType.AudioApp when this.Request.IncludeSystemAudio => AssetWriterTrack.SystemAudio,
                RPSampleBufferType.AudioMic when this.Request.IncludeMicrophone => AssetWriterTrack.Microphone,
                _ => (AssetWriterTrack?)null
            };

            if (track == null)
                return;

            var writer = this.EnsureSink(buffer, track.Value);
            writer?.Append(buffer, track.Value);
        }
        catch (Exception ex)
        {
            this.OnPlatformStopped(ScreenRecordingFaultReason.EncoderFailed, ex);
        }
    }


    // the sink cannot exist before the first video buffer, because that buffer is what tells us the
    // capture resolution; audio arriving first is dropped rather than buffered, which costs a few
    // milliseconds of leading sound and avoids a whole class of writer-session ordering failures
    AssetWriterSink? EnsureSink(CMSampleBuffer buffer, AssetWriterTrack track)
    {
        lock (this.sinkGate)
        {
            if (this.sink != null)
                return this.sink;

            if (track != AssetWriterTrack.Video)
                return null;

            using var image = buffer.GetImageBuffer() as CVPixelBuffer;
            if (image == null)
                return null;

            this.dimensions = VideoDimensions.From(this.Request, (int)image.Width, (int)image.Height);
            this.Logger.CaptureConfigured(this.dimensions.Width, this.dimensions.Height, this.dimensions.FrameRate, this.dimensions.Bitrate);

            this.sink = new AssetWriterSink(
                this.outputPath,
                this.dimensions,
                this.Request.IncludeSystemAudio,
                this.Request.IncludeMicrophone,
                this.Logger
            );

            return this.sink;
        }
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
        await StopCapture().ConfigureAwait(false);

        AssetWriterSink? writer;
        lock (this.sinkGate)
            writer = this.sink;

        if (writer == null)
            throw new ScreenRecorderException("ReplayKit never delivered a video frame - nothing was recorded");

        await writer.Finish().ConfigureAwait(false);
        this.Teardown();

        var info = new FileInfo(this.outputPath);
        if (!info.Exists)
            throw new ScreenRecorderException("ReplayKit stopped without producing a file");

        return new ScreenRecordingResult
        {
            FilePath = this.outputPath,
            Duration = writer.Duration == TimeSpan.Zero ? this.Elapsed : writer.Duration,
            ByteSize = info.Length,
            Width = this.dimensions.Width,
            Height = this.dimensions.Height,
            MimeType = "video/mp4"
        };
    }


    protected override async Task OnCancel(CancellationToken ct)
    {
        await StopCapture().ConfigureAwait(false);
        this.sink?.Abort();
        this.Teardown();
    }


    static async Task StopCapture()
    {
        try
        {
            await RPScreenRecorder.SharedRecorder.StopCaptureAsync().ConfigureAwait(false);
        }
        catch (NSErrorException)
        {
            // ReplayKit reports an error when it had already stopped itself, which is exactly the
            // path a fault takes to get here - the writer is finalised either way
        }
    }


    void Teardown()
    {
        lock (this.sinkGate)
        {
            this.sink?.Dispose();
            this.sink = null;
        }

        if (ReferenceEquals(RPScreenRecorder.SharedRecorder.Delegate, this.recorderDelegate))
            RPScreenRecorder.SharedRecorder.Delegate = null!;
    }


    static Exception Translate(NSError error) => (RPRecordingError)(long)error.Code switch
    {
        RPRecordingError.UserDeclined => new ScreenRecorderPermissionException("The user declined the screen recording prompt"),
        RPRecordingError.Disabled => new ScreenRecorderPermissionException("Screen recording is disabled on this device, usually by a Screen Time or MDM restriction"),
        RPRecordingError.Entitlements => new ScreenRecorderPermissionException($"ReplayKit rejected the app's entitlements - {error.LocalizedDescription}"),
        RPRecordingError.InsufficientStorage => new ScreenRecorderException("There is not enough free storage to record"),
        RPRecordingError.ActivePhoneCall => new ScreenRecorderException("A call is in progress - ReplayKit will not record during one"),
        _ => new ScreenRecorderException($"ReplayKit could not start - {error.LocalizedDescription}")
    };


    sealed class RecorderDelegate(AppleScreenRecording owner) : RPScreenRecorderDelegate
    {
        public override void DidStopRecording(RPScreenRecorder screenRecorder, RPPreviewViewController? previewViewController, NSError? error)
        {
            // ReplayKit signals a clean, app-initiated stop with a null error - that path is
            // already being handled by whoever called Stop, so there is no fault to raise
            if (error == null)
                return;

            var reason = (RPRecordingError)(long)error.Code switch
            {
                RPRecordingError.ActivePhoneCall or RPRecordingError.Interrupted or RPRecordingError.SystemDormancy
                    => ScreenRecordingFaultReason.InterruptedBySystem,
                RPRecordingError.UserDeclined => ScreenRecordingFaultReason.RevokedByUser,
                RPRecordingError.ContentResize => ScreenRecordingFaultReason.TargetLost,
                _ => ScreenRecordingFaultReason.Unknown
            };

            owner.OnPlatformStopped(reason, new NSErrorException(error));
        }
    }
}

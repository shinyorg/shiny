using Microsoft.Extensions.Logging;
using Shiny.ScreenRecorder.Infrastructure;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;

namespace Shiny.ScreenRecorder;


/// <summary>
/// A live Windows.Graphics.Capture session bridged into a <see cref="MediaStreamSource"/> that
/// <see cref="MediaTranscoder"/> encodes to MP4.
/// </summary>
/// <remarks>
/// <para>The two halves run on opposite schedules and have to be bridged: the frame pool
/// <em>pushes</em> frames as the compositor produces them, while the transcoder <em>pulls</em>
/// samples when its encoder is ready for one. So a sample request that arrives with no frame in
/// hand takes a deferral and is parked until the next frame arrives, and a frame that arrives with
/// nothing waiting for it is dropped. Buffering instead would grow without bound whenever the
/// encoder fell behind, which on a 4K display it eventually does.</para>
/// <para>Pausing simply stops satisfying requests. The transcoder waits, the frames that arrive
/// meanwhile are discarded, and the span they covered is subtracted from every later timestamp -
/// so the output has no frozen stretch in the middle.</para>
/// </remarks>
class WindowsScreenRecording : AbstractScreenRecording
{
    readonly object gate = new();
    readonly GraphicsCaptureItem item;
    readonly VideoDimensions dimensions;
    readonly string outputPath;

    IDirect3DDevice? device;
    Direct3D11CaptureFramePool? framePool;
    GraphicsCaptureSession? session;
    MediaStreamSource? source;
    Stream? output;
    Task? transcode;

    MediaStreamSourceSampleRequest? pendingRequest;
    MediaStreamSourceSampleRequestDeferral? pendingDeferral;

    TimeSpan startTime = TimeSpan.MinValue;
    TimeSpan pausedTotal = TimeSpan.Zero;
    TimeSpan pauseStartedAt = TimeSpan.MinValue;
    TimeSpan lastTimestamp = TimeSpan.Zero;
    bool paused;
    bool ended;


    public WindowsScreenRecording(
        ScreenRecordingRequest request,
        ScreenRecorderCapabilities capabilities,
        string platformReason,
        GraphicsCaptureItem item,
        VideoDimensions dimensions,
        string outputPath,
        ILogger logger
    ) : base(request, capabilities, platformReason, logger)
    {
        this.item = item;
        this.dimensions = dimensions;
        this.outputPath = outputPath;
    }


    protected override string? OutputFilePath => this.outputPath;


    public Task Start(CancellationToken ct)
    {
        this.device = Direct3DInterop.CreateDevice();

        // two buffers is enough for a pull-driven consumer and keeps the pool's memory down on a
        // large display; the free-threaded pool is required because FrameArrived must not need a
        // DispatcherQueue - there may not be one on the calling thread
        this.framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            this.device,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            2,
            this.item.Size
        );

        this.framePool.FrameArrived += this.OnFrameArrived;
        this.item.Closed += this.OnItemClosed;

        this.session = this.framePool.CreateCaptureSession(this.item);
        this.session.IsCursorCaptureEnabled = this.Request.ShowCursor;

        this.source = this.BuildSource();

        if (File.Exists(this.outputPath))
            File.Delete(this.outputPath);

        this.output = File.Create(this.outputPath);

        this.session.StartCapture();
        this.transcode = this.RunTranscode();
        this.BeginClock();

        return Task.CompletedTask;
    }


    MediaStreamSource BuildSource()
    {
        var properties = VideoEncodingProperties.CreateUncompressed(
            MediaEncodingSubtypes.Bgra8,
            (uint)this.item.Size.Width,
            (uint)this.item.Size.Height
        );

        var descriptor = new VideoStreamDescriptor(properties);
        var source = new MediaStreamSource(descriptor)
        {
            // a live source with no buffering: the transcoder must not sit on frames waiting to
            // build a buffer that will never fill, because the screen may not change for minutes
            BufferTime = TimeSpan.Zero,
            IsLive = true
        };

        source.Starting += (_, args) => args.Request.SetActualStartPosition(TimeSpan.Zero);
        source.SampleRequested += this.OnSampleRequested;

        return source;
    }


    async Task RunTranscode()
    {
        try
        {
            var profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD1080p);
            profile.Video.Width = (uint)this.dimensions.Width;
            profile.Video.Height = (uint)this.dimensions.Height;
            profile.Video.Bitrate = (uint)this.dimensions.Bitrate;
            profile.Video.FrameRate.Numerator = (uint)this.dimensions.FrameRate;
            profile.Video.FrameRate.Denominator = 1;

            // the capture has no audio track at all, so leaving the profile's audio stream in place
            // would make the transcoder wait forever for samples that never come
            profile.Audio = null;

            var transcoder = new MediaTranscoder { HardwareAccelerationEnabled = true };
            var prepared = await transcoder
                .PrepareMediaStreamSourceTranscodeAsync(this.source, this.output!.AsRandomAccessStream(), profile)
                .AsTask()
                .ConfigureAwait(false);

            if (!prepared.CanTranscode)
                throw new ScreenRecorderException($"Windows cannot encode this capture - {prepared.FailureReason}");

            await prepared.TranscodeAsync().AsTask().ConfigureAwait(false);
        }
        catch (Exception ex) when (!this.IsFinished)
        {
            this.OnPlatformStopped(ScreenRecordingFaultReason.EncoderFailed, ex);
        }
    }


    void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        using var frame = sender.TryGetNextFrame();
        if (frame == null)
            return;

        MediaStreamSourceSampleRequest? request;
        MediaStreamSourceSampleRequestDeferral? deferral;
        TimeSpan timestamp;

        lock (this.gate)
        {
            if (this.ended)
                return;

            if (this.startTime == TimeSpan.MinValue)
                this.startTime = frame.SystemRelativeTime;

            if (this.paused)
            {
                // remember where the pause began so Resume knows how much to discount
                if (this.pauseStartedAt == TimeSpan.MinValue)
                    this.pauseStartedAt = frame.SystemRelativeTime;

                return;
            }

            // nothing is waiting for a frame, so this one is dropped rather than queued - the
            // encoder is behind and buffering would only make it further behind
            if (this.pendingRequest == null)
                return;

            timestamp = frame.SystemRelativeTime - this.startTime - this.pausedTotal;
            if (timestamp < TimeSpan.Zero)
                timestamp = TimeSpan.Zero;

            this.lastTimestamp = timestamp;

            request = this.pendingRequest;
            deferral = this.pendingDeferral;
            this.pendingRequest = null;
            this.pendingDeferral = null;
        }

        try
        {
            // the sample takes its own reference on the surface, so disposing the frame afterwards
            // is safe and is what returns the buffer to the pool
            request.Sample = MediaStreamSample.CreateFromDirect3D11Surface(frame.Surface, timestamp);
        }
        catch (Exception ex)
        {
            this.Logger.SampleCreationFailed(ex);
            request.Sample = null;
        }
        finally
        {
            deferral?.Complete();
        }
    }


    void OnSampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
    {
        lock (this.gate)
        {
            if (this.ended)
            {
                // a null sample is how a MediaStreamSource signals end of stream, which is what
                // lets the transcoder finalise the file
                args.Request.Sample = null;
                return;
            }

            this.pendingRequest = args.Request;
            this.pendingDeferral = args.Request.GetDeferral();
        }
    }


    void OnItemClosed(GraphicsCaptureItem sender, object args)
        => this.OnPlatformStopped(ScreenRecordingFaultReason.TargetLost, null);


    protected override Task OnPause(CancellationToken ct)
    {
        lock (this.gate)
        {
            this.paused = true;
            this.pauseStartedAt = TimeSpan.MinValue;
        }

        return Task.CompletedTask;
    }


    protected override Task OnResume(CancellationToken ct)
    {
        lock (this.gate)
        {
            if (!this.paused)
                return Task.CompletedTask;

            this.paused = false;

            if (this.pauseStartedAt != TimeSpan.MinValue)
            {
                // the gap runs from the first dropped frame to the last one written, measured on
                // the capture's own clock, so the timeline closes up exactly
                var skipped = this.pauseStartedAt - this.startTime - this.pausedTotal - this.lastTimestamp;
                if (skipped > TimeSpan.Zero)
                    this.pausedTotal += skipped;

                this.pauseStartedAt = TimeSpan.MinValue;
            }
        }

        return Task.CompletedTask;
    }


    protected override async Task<ScreenRecordingResult> OnStop(CancellationToken ct)
    {
        this.EndStream();

        // the transcoder finalises the MP4 index once it sees the end-of-stream sample; killing it
        // early leaves a file with no moov atom, which no player will open
        if (this.transcode != null)
        {
            try
            {
                await this.transcode.WaitAsync(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                this.Logger.TranscodeDidNotFinish();
            }
        }

        this.Teardown();

        var info = new FileInfo(this.outputPath);
        if (!info.Exists || info.Length == 0)
            throw new ScreenRecorderException("The transcoder produced no usable file");

        return new ScreenRecordingResult
        {
            FilePath = this.outputPath,
            Duration = this.lastTimestamp == TimeSpan.Zero ? this.Elapsed : this.lastTimestamp,
            ByteSize = info.Length,
            Width = this.dimensions.Width,
            Height = this.dimensions.Height,
            MimeType = "video/mp4"
        };
    }


    protected override Task OnCancel(CancellationToken ct)
    {
        this.EndStream();
        this.Teardown();

        return Task.CompletedTask;
    }


    void EndStream()
    {
        MediaStreamSourceSampleRequest? request;
        MediaStreamSourceSampleRequestDeferral? deferral;

        lock (this.gate)
        {
            this.ended = true;
            request = this.pendingRequest;
            deferral = this.pendingDeferral;
            this.pendingRequest = null;
            this.pendingDeferral = null;
        }

        // a request parked when the stop arrived must be released, or the transcoder waits forever
        if (request != null)
            request.Sample = null;

        deferral?.Complete();

        try
        {
            this.session?.Dispose();
            this.session = null;
        }
        catch (Exception ex)
        {
            this.Logger.CaptureSessionDisposeFailed(ex);
        }
    }


    void Teardown()
    {
        if (this.framePool != null)
        {
            this.framePool.FrameArrived -= this.OnFrameArrived;
            this.framePool.Dispose();
            this.framePool = null;
        }

        this.item.Closed -= this.OnItemClosed;

        if (this.source != null)
        {
            this.source.SampleRequested -= this.OnSampleRequested;
            this.source = null;
        }

        this.output?.Dispose();
        this.output = null;

        this.device?.Dispose();
        this.device = null;
    }
}

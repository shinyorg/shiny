using Android.Hardware.Display;
using Android.Media.Projection;
using Android.Views;
using Microsoft.Extensions.Logging;
using Shiny.ScreenRecorder.Infrastructure;

namespace Shiny.ScreenRecorder;


/// <summary>
/// A live MediaProjection capture: a VirtualDisplay rendering into an H.264 encoder's surface, with
/// an optional AAC track, both muxed into one MP4.
/// </summary>
class AndroidScreenRecording : AbstractScreenRecording
{
    readonly AndroidPlatform platform;
    readonly MediaProjection projection;
    readonly VideoDimensions dimensions;
    readonly int densityDpi;
    readonly string outputPath;
    readonly ProjectionCallback projectionCallback;

    MediaMuxerSink? muxer;
    VideoSurfaceEncoder? video;
    AudioTrackEncoder? audio;
    VirtualDisplay? display;


    public AndroidScreenRecording(
        ScreenRecordingRequest request,
        ScreenRecorderCapabilities capabilities,
        string platformReason,
        AndroidPlatform platform,
        MediaProjection projection,
        VideoDimensions dimensions,
        int densityDpi,
        string outputPath,
        ILogger logger
    ) : base(request, capabilities, platformReason, logger)
    {
        this.platform = platform;
        this.projection = projection;
        this.dimensions = dimensions;
        this.densityDpi = densityDpi;
        this.outputPath = outputPath;
        this.projectionCallback = new ProjectionCallback(this);
    }


    protected override string? OutputFilePath => this.outputPath;


    public void Start()
    {
        var withAudio = this.Request.IncludeMicrophone || this.Request.IncludeSystemAudio;

        if (File.Exists(this.outputPath))
            File.Delete(this.outputPath);

        // registering before the projection is used at all - a projection revoked between here and
        // the first frame otherwise goes unnoticed
        this.projection.RegisterCallback(this.projectionCallback, null);

        this.muxer = new MediaMuxerSink(this.outputPath, withAudio ? 2 : 1, this.Logger);
        this.video = new VideoSurfaceEncoder(this.dimensions, this.muxer, this.Logger);

        if (withAudio)
        {
            this.audio = new AudioTrackEncoder(
                this.projection,
                this.Request.IncludeMicrophone,
                this.Request.IncludeSystemAudio,
                this.muxer,
                this.Logger
            );
        }

        this.display = this.projection.CreateVirtualDisplay(
            "shiny-screen-recorder",
            this.dimensions.Width,
            this.dimensions.Height,
            this.densityDpi,

            // AutoMirror makes the virtual display show whatever the default display shows, which
            // is the whole point of screen recording; Public lets other apps' content reach it.
            // The binding types this parameter as Android.Views.DisplayFlags, but the values the
            // platform actually documents for createVirtualDisplay live on VirtualDisplayFlags -
            // the same underlying ints, in a different managed enum.
            (DisplayFlags)(VirtualDisplayFlags.AutoMirror | VirtualDisplayFlags.Public),
            this.video.InputSurface,
            null,
            null
        ) ?? throw new ScreenRecorderException("Android refused to create the virtual display");

        this.BeginClock();
    }


    protected override Task OnPause(CancellationToken ct)
    {
        this.video?.Pause();
        this.audio?.Pause();
        return Task.CompletedTask;
    }


    protected override Task OnResume(CancellationToken ct)
    {
        this.video?.Resume();
        this.audio?.Resume();
        return Task.CompletedTask;
    }


    protected override Task<ScreenRecordingResult> OnStop(CancellationToken ct)
    {
        var lastFrameUs = this.video?.LastWrittenUs ?? -1;

        // order matters on the way down: the display must stop producing frames before the encoder
        // is flushed, and both encoders must be flushed before the muxer writes its index
        this.display?.Release();
        this.display = null;

        this.video?.Stop();
        this.audio?.Stop();

        var wrote = this.muxer?.Stop() ?? false;
        this.Teardown();

        if (!wrote || !File.Exists(this.outputPath))
            throw new ScreenRecorderException("The recording produced no usable file - the encoder never delivered a frame");

        var info = new FileInfo(this.outputPath);
        var duration = lastFrameUs > 0 ? TimeSpan.FromMilliseconds(lastFrameUs / 1000d) : this.Elapsed;

        return Task.FromResult(new ScreenRecordingResult
        {
            FilePath = this.outputPath,
            Duration = duration,
            ByteSize = info.Length,
            Width = this.dimensions.Width,
            Height = this.dimensions.Height,
            MimeType = "video/mp4"
        });
    }


    protected override Task OnCancel(CancellationToken ct)
    {
        this.display?.Release();
        this.display = null;
        this.video?.Stop();
        this.audio?.Stop();
        this.muxer?.Stop();
        this.Teardown();

        return Task.CompletedTask;
    }


    void Teardown()
    {
        this.video?.Dispose();
        this.video = null;
        this.audio?.Dispose();
        this.audio = null;
        this.muxer?.Dispose();
        this.muxer = null;

        try
        {
            this.projection.UnregisterCallback(this.projectionCallback);
            this.projection.Stop();
        }
        catch (Exception ex)
        {
            this.Logger.ProjectionTeardownFailed(ex);
        }

        // the foreground service exists only for the life of the projection; leaving it running
        // would keep an ongoing notification on screen for a recording that finished
        ScreenRecorderService.StopService(this.platform);
    }


    sealed class ProjectionCallback(AndroidScreenRecording owner) : MediaProjection.Callback
    {
        public override void OnStop()
            => owner.OnPlatformStopped(ScreenRecordingFaultReason.RevokedByUser, null);
    }
}

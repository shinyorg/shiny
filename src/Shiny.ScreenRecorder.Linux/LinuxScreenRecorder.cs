using Microsoft.Extensions.Logging;
using Shiny.ScreenRecorder.Encoders;
using Shiny.ScreenRecorder.Infrastructure;
using Shiny.ScreenRecorder.Portal;

namespace Shiny.ScreenRecorder;


/// <summary>
/// The Linux implementation - the xdg-desktop-portal ScreenCast API for consent and frames,
/// GStreamer or FFmpeg for encoding.
/// </summary>
/// <remarks>
/// <para>Unlike every other platform in this module, the encoder is a child process rather than a
/// library. There is no managed way to consume a PipeWire stream, and the alternative - binding
/// libgstreamer or libpipewire - is a large permanent interop surface for something
/// <c>gst-launch-1.0</c> already does correctly.</para>
/// <para><b>The compositor owns target selection.</b> The portal shows its own picker during
/// <see cref="IScreenRecorder.Start"/> and the user chooses there, so
/// <see cref="GetTargets"/> throws and <see cref="ScreenRecordingRequest.Target"/> must be
/// null.</para>
/// <para>Requirements: a desktop session with a running <c>xdg-desktop-portal</c> that implements
/// ScreenCast (GNOME, KDE and wlroots all do), plus <c>gst-launch-1.0</c> with the good/bad plugin
/// sets, or <c>ffmpeg</c> on X11. <see cref="Capabilities"/> is probed from what is actually
/// installed, so a machine missing the pieces reports
/// <see cref="ScreenRecorderCapabilities.None"/> rather than failing at record time.</para>
/// </remarks>
public class LinuxScreenRecorder : AbstractScreenRecorder, IAsyncDisposable
{
    readonly ILogger<LinuxScreenRecorder> logger;
    readonly Lazy<EncoderKind> encoder;
    readonly Lazy<bool> hasPulseAudio;


    public LinuxScreenRecorder(ILogger<LinuxScreenRecorder> logger) : base(logger)
    {
        this.logger = logger;

        // probing spawns processes, so it happens once and lazily rather than in the constructor -
        // a DI container building the graph should not be shelling out
        this.encoder = new Lazy<EncoderKind>(DetectEncoder);
        this.hasPulseAudio = new Lazy<bool>(EncoderProbe.HasPulseAudio);
    }


    static EncoderKind DetectEncoder()
    {
        if (EncoderProbe.Exists("gst-launch-1.0"))
            return EncoderKind.GStreamer;

        // x11grab only exists on X11; on Wayland an installed ffmpeg is no help at all
        if (EncoderProbe.IsX11 && EncoderProbe.Exists("ffmpeg"))
            return EncoderKind.FfmpegX11;

        return EncoderKind.None;
    }


    public override ScreenRecorderCapabilities Capabilities
    {
        get
        {
            if (this.encoder.Value == EncoderKind.None)
                return ScreenRecorderCapabilities.None;

            var caps = ScreenRecorderCapabilities.Recording
                | ScreenRecorderCapabilities.CursorToggle
                | ScreenRecorderCapabilities.FrameRateControl
                | ScreenRecorderCapabilities.BitrateControl
                | ScreenRecorderCapabilities.Downscaling;

            if (this.hasPulseAudio.Value)
            {
                caps |= ScreenRecorderCapabilities.Microphone;

                // system audio is a monitor source, which only GStreamer's pulsesrc can be pointed
                // at by name; the ffmpeg fallback records the default input instead
                if (this.encoder.Value == EncoderKind.GStreamer)
                    caps |= ScreenRecorderCapabilities.SystemAudio;
            }

            return caps;
        }
    }


    protected override string PlatformReason => this.encoder.Value == EncoderKind.None
        ? "no usable encoder was found - install gstreamer1.0-tools with the good and bad plugin sets, or ffmpeg on an X11 session"
        : "the desktop portal runs its own picker and does not disclose a list of displays or windows, and neither gst-launch nor ffmpeg can pause a running pipeline";


    public override async Task<AccessState> RequestAccess(ScreenRecordingRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (this.encoder.Value == EncoderKind.None)
            return AccessState.NotSupported;

        if (this.encoder.Value == EncoderKind.FfmpegX11)
            return AccessState.Available;

        await using var portal = new ScreenCastPortal(this.logger);
        var available = await portal.IsAvailable(ct).ConfigureAwait(false);

        // the portal shows its consent picker per recording and cannot be pre-approved, so this
        // only answers whether a portal is there to ask
        return available ? AccessState.Unknown : AccessState.NotSupported;
    }


    protected override async Task<AbstractScreenRecording> OnStart(
        ScreenRecordingRequest request,
        string? outputPath,
        CancellationToken ct
    )
    {
        if (this.encoder.Value == EncoderKind.None)
            throw ScreenRecorderNotSupportedException.For(ScreenRecorderCapabilities.Recording, this.PlatformReason);

        if (request.Target != null)
            throw ScreenRecorderNotSupportedException.For(ScreenRecorderCapabilities.DisplaySelection, this.PlatformReason);

        return this.encoder.Value == EncoderKind.GStreamer
            ? await this.StartGStreamer(request, outputPath!, ct).ConfigureAwait(false)
            : this.StartFfmpeg(request, outputPath!);
    }


    async Task<AbstractScreenRecording> StartGStreamer(ScreenRecordingRequest request, string outputPath, CancellationToken ct)
    {
        var portal = new ScreenCastPortal(this.logger);
        try
        {
            var stream = await portal.Start(request.ShowCursor, ct).ConfigureAwait(false);

            // the portal's size hint is the real capture size where it is given; without it the
            // pipeline is sized from the request alone and PipeWire negotiates the rest
            var dimensions = VideoDimensions.From(
                request,
                stream.Width ?? request.MaxWidth ?? 1920,
                stream.Height ?? 1080
            );

            var monitor = request.IncludeSystemAudio ? EncoderProbe.GetDefaultMonitorSource() : null;
            if (request.IncludeSystemAudio && monitor == null)
                throw new ScreenRecorderException("System audio was requested but no PulseAudio default sink could be found to monitor");

            var command = EncoderCommandBuilder.GStreamer(
                stream.NodeId,
                dimensions,
                outputPath,
                monitor,
                request.IncludeMicrophone
            );

            this.logger.EncoderCommandBuilt(command.Display);

            var recording = new LinuxScreenRecording(
                request,
                this.Capabilities,
                this.PlatformReason,
                command,
                dimensions,
                outputPath,
                portal,
                this.logger
            );

            recording.Start();

            return recording;
        }
        catch
        {
            await portal.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }


    AbstractScreenRecording StartFfmpeg(ScreenRecordingRequest request, string outputPath)
    {
        // x11grab captures the whole display, so the request's MaxWidth is applied as a scale
        // filter and the native size is whatever X reports through the grab itself
        var dimensions = VideoDimensions.From(request, request.MaxWidth ?? 1920, 1080);

        var command = EncoderCommandBuilder.FfmpegX11(
            EncoderProbe.Display,
            dimensions,
            outputPath,
            request.ShowCursor,
            request.IncludeMicrophone
        );

        this.logger.EncoderCommandBuilt(command.Display);

        var recording = new LinuxScreenRecording(
            request,
            this.Capabilities,
            this.PlatformReason,
            command,
            dimensions,
            outputPath,
            null,
            this.logger
        );

        recording.Start();

        return recording;
    }


    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

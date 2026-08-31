using Microsoft.Extensions.Logging;
using Windows.Graphics.Capture;
using Shiny.ScreenRecorder.Infrastructure;

namespace Shiny.ScreenRecorder;


/// <summary>
/// The Windows implementation, backed by Windows.Graphics.Capture and Media Foundation.
/// </summary>
/// <remarks>
/// <para>Frames come out of a <c>Direct3D11CaptureFramePool</c> as GPU surfaces and go into a
/// <c>MediaStreamSource</c>, which <c>MediaTranscoder</c> encodes to MP4. No frame is ever copied
/// into managed memory.</para>
/// <para><b>There is no audio.</b> Windows.Graphics.Capture captures pixels and nothing else -
/// unlike every other platform in this module, its capture API has no audio path at all. System
/// audio would mean a hand-written WASAPI loopback capture and a second encoded stream, which is a
/// separate piece of work. <see cref="Capabilities"/> reports neither
/// <see cref="ScreenRecorderCapabilities.Microphone"/> nor
/// <see cref="ScreenRecorderCapabilities.SystemAudio"/>, and asking for either throws rather than
/// silently recording a silent file.</para>
/// <para>Windows 11 draws a yellow border around whatever is being captured. From Windows 11
/// 24H2 an app may turn it off, and this does not - a recording indicator the user can see is the
/// right default, and nothing here tries to hide one.</para>
/// </remarks>
public class WindowsScreenRecorder(ILogger<WindowsScreenRecorder> logger) : AbstractScreenRecorder(logger)
{
    public override ScreenRecorderCapabilities Capabilities
    {
        get
        {
            if (!GraphicsCaptureSession.IsSupported())
                return ScreenRecorderCapabilities.None;

            return ScreenRecorderCapabilities.Recording
                | ScreenRecorderCapabilities.PauseResume
                | ScreenRecorderCapabilities.DisplaySelection
                | ScreenRecorderCapabilities.WindowSelection
                | ScreenRecorderCapabilities.CursorToggle
                | ScreenRecorderCapabilities.FrameRateControl
                | ScreenRecorderCapabilities.BitrateControl
                | ScreenRecorderCapabilities.Downscaling;
        }
    }


    protected override string PlatformReason =>
        "Windows.Graphics.Capture captures pixels only - it has no audio path, and adding one means a separate WASAPI loopback capture";


    public override Task<AccessState> RequestAccess(ScreenRecordingRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!GraphicsCaptureSession.IsSupported())
            return Task.FromResult(AccessState.NotSupported);

        // there is no permission prompt for graphics capture on Windows; a packaged app either
        // declared the graphicsCapture capability at build time or it did not
        if (request.IncludeMicrophone || request.IncludeSystemAudio)
            return Task.FromResult(AccessState.NotSupported);

        return Task.FromResult(AccessState.Available);
    }


    public override Task<IReadOnlyList<CaptureTarget>> GetTargets(CancellationToken ct = default)
    {
        this.AssertCapability(ScreenRecorderCapabilities.DisplaySelection);

        var targets = new List<CaptureTarget>();

        foreach (var monitor in CaptureItemInterop.GetMonitors())
        {
            targets.Add(new CaptureTarget
            {
                Id = FormatId(CaptureTargetKind.Display, monitor.Handle),
                Kind = CaptureTargetKind.Display,
                Name = monitor.Name,
                Width = monitor.Width,
                Height = monitor.Height,
                IsPrimary = monitor.IsPrimary
            });
        }

        foreach (var window in CaptureItemInterop.GetWindows())
        {
            targets.Add(new CaptureTarget
            {
                Id = FormatId(CaptureTargetKind.Window, window.Handle),
                Kind = CaptureTargetKind.Window,
                Name = window.Title,
                Width = window.Width,
                Height = window.Height
            });
        }

        this.Logger.TargetsEnumerated(targets.Count);

        return Task.FromResult<IReadOnlyList<CaptureTarget>>(targets);
    }


    protected override async Task<AbstractScreenRecording> OnStart(
        ScreenRecordingRequest request,
        string? outputPath,
        CancellationToken ct
    )
    {
        if (!GraphicsCaptureSession.IsSupported())
            throw ScreenRecorderNotSupportedException.For(ScreenRecorderCapabilities.Recording, "Windows.Graphics.Capture needs Windows 10 1903 or later");

        var item = this.ResolveItem(request.Target);

        // the capture item reports the source size; a window that is later resized keeps encoding
        // at the size it started, because the encoder's stream descriptor is fixed for the file
        var dimensions = VideoDimensions.From(request, item.Size.Width, item.Size.Height);
        this.Logger.CaptureConfigured(dimensions.Width, dimensions.Height, dimensions.FrameRate, dimensions.Bitrate);

        var recording = new WindowsScreenRecording(
            request,
            this.Capabilities,
            this.PlatformReason,
            item,
            dimensions,
            outputPath!,
            logger
        );

        await recording.Start(ct).ConfigureAwait(false);

        return recording;
    }


    GraphicsCaptureItem ResolveItem(CaptureTarget? target)
    {
        if (target == null)
        {
            var primary = CaptureItemInterop.GetMonitors().FirstOrDefault(m => m.IsPrimary)
                ?? CaptureItemInterop.GetMonitors().FirstOrDefault()
                ?? throw new ScreenRecorderException("Windows reported no displays to record");

            return CaptureItemInterop.CreateForMonitor(primary.Handle);
        }

        var (kind, handle) = ParseId(target.Id);

        return kind == CaptureTargetKind.Display
            ? CaptureItemInterop.CreateForMonitor(handle)
            : CaptureItemInterop.CreateForWindow(handle);
    }


    // HMONITOR and HWND come from different namespaces and can collide numerically, so the kind
    // travels with the handle
    static string FormatId(CaptureTargetKind kind, IntPtr handle) => $"{kind}:{handle.ToInt64()}";

    static (CaptureTargetKind Kind, IntPtr Handle) ParseId(string id)
    {
        var parts = id.Split(':', 2);

        if (parts.Length == 2 && Enum.TryParse<CaptureTargetKind>(parts[0], out var kind) && Int64.TryParse(parts[1], out var value))
            return (kind, new IntPtr(value));

        throw new ScreenRecorderException($"'{id}' is not a capture target id produced by GetTargets");
    }
}

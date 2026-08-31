using AVFoundation;
using Foundation;
using Microsoft.Extensions.Logging;
using ScreenCaptureKit;
using Shiny.ScreenRecorder.Infrastructure;

namespace Shiny.ScreenRecorder;


/// <summary>
/// The macOS implementation, backed by ScreenCaptureKit.
/// </summary>
/// <remarks>
/// <para>The richest backend in the module - macOS is the only platform that will enumerate
/// displays, windows *and* applications, capture the machine's whole audio output, and exclude the
/// recording app's own sound from what it captures.</para>
/// <para><b>There are two recording paths and they differ in what they can do.</b> macOS 15
/// introduced <c>SCRecordingOutput</c>, which writes the MP4 itself - no sample pump, no
/// AVAssetWriter, no timestamp arithmetic, and microphone capture handled by the OS. It cannot be
/// paused, because detaching and reattaching a recording output mid-file does not produce one
/// continuous movie. Below macOS 15 the stream's sample buffers go through
/// <see cref="AssetWriterSink"/> instead, which *can* synthesise a pause. So
/// <see cref="ScreenRecorderCapabilities.PauseResume"/> is present on older macOS and absent on
/// newer - deliberately, rather than crippling the modern path to make the flags uniform.</para>
/// </remarks>
public class MacOSScreenRecorder(ILogger<MacOSScreenRecorder> logger) : AbstractScreenRecorder(logger)
{
    // SCRecordingOutput is macOS 15+; below that the stream hands over sample buffers and we write
    // the file ourselves
    static bool UseRecordingOutput => OperatingSystem.IsMacOSVersionAtLeast(15);


    public override ScreenRecorderCapabilities Capabilities
    {
        get
        {
            if (!OperatingSystem.IsMacOSVersionAtLeast(12, 3))
                return ScreenRecorderCapabilities.None;

            var caps = ScreenRecorderCapabilities.Recording
                | ScreenRecorderCapabilities.SystemAudio
                | ScreenRecorderCapabilities.DisplaySelection
                | ScreenRecorderCapabilities.WindowSelection
                | ScreenRecorderCapabilities.CursorToggle
                | ScreenRecorderCapabilities.FrameRateControl
                | ScreenRecorderCapabilities.BitrateControl
                | ScreenRecorderCapabilities.Downscaling;

            if (UseRecordingOutput)
            {
                // SCStreamConfiguration.CaptureMicrophone landed alongside SCRecordingOutput; the
                // older path has no microphone source at all, because ScreenCaptureKit below 15
                // only produces screen and system audio
                caps |= ScreenRecorderCapabilities.Microphone;
            }
            else
            {
                caps |= ScreenRecorderCapabilities.PauseResume;
            }

            return caps;
        }
    }


    protected override string PlatformReason => UseRecordingOutput
        ? "macOS 15 records through SCRecordingOutput, which writes the file itself and cannot be paused or reconfigured mid-recording"
        : "ScreenCaptureKit below macOS 15 captures the screen and system audio only - it has no microphone source";


    public override async Task<AccessState> RequestAccess(ScreenRecordingRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!OperatingSystem.IsMacOSVersionAtLeast(12, 3))
            return AccessState.NotSupported;

        if (!CoreGraphicsScreenCapture.Preflight() && !CoreGraphicsScreenCapture.Request())
        {
            // the first Request() after a grant still answers false - macOS only applies a new
            // Screen Recording grant on the next launch - so this is "not yet", not "never"
            this.Logger.PermissionRefused("Screen Recording is not granted. Approve it in System Settings > Privacy & Security > Screen Recording, then relaunch the app");
            return AccessState.Denied;
        }

        if (!request.IncludeMicrophone)
            return AccessState.Available;

        var mic = await AVCaptureDevice
            .RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Audio)
            .ConfigureAwait(false);

        return mic ? AccessState.Available : AccessState.Denied;
    }


    public override async Task<IReadOnlyList<CaptureTarget>> GetTargets(CancellationToken ct = default)
    {
        this.AssertCapability(ScreenRecorderCapabilities.DisplaySelection);

        var content = await GetShareableContent().ConfigureAwait(false);
        var targets = new List<CaptureTarget>();

        foreach (var display in content.Displays)
        {
            targets.Add(new CaptureTarget
            {
                Id = FormatId(CaptureTargetKind.Display, display.DisplayId),
                Kind = CaptureTargetKind.Display,
                Name = content.Displays.Length == 1 ? "Display" : $"Display {display.DisplayId}",
                Width = (int)display.Width,
                Height = (int)display.Height,

                // CGMainDisplayID is always the one at the origin; SCDisplay does not flag it
                IsPrimary = display.Frame.X == 0 && display.Frame.Y == 0
            });
        }

        foreach (var window in content.Windows)
        {
            // windows with no title are almost always shadows, tooltips and other chrome the user
            // has no way to recognise in a picker
            if (String.IsNullOrWhiteSpace(window.Title) || !window.OnScreen)
                continue;

            targets.Add(new CaptureTarget
            {
                Id = FormatId(CaptureTargetKind.Window, window.WindowId),
                Kind = CaptureTargetKind.Window,
                Name = window.Title,
                ApplicationName = window.OwningApplication?.ApplicationName,
                Width = (int)window.Frame.Width,
                Height = (int)window.Frame.Height
            });
        }

        this.Logger.TargetsEnumerated(targets.Count);

        return targets;
    }


    protected override async Task<AbstractScreenRecording> OnStart(
        ScreenRecordingRequest request,
        string? outputPath,
        CancellationToken ct
    )
    {
        var content = await GetShareableContent().ConfigureAwait(false);
        var (filter, nativeWidth, nativeHeight) = BuildFilter(content, request.Target);
        var dimensions = VideoDimensions.From(request, nativeWidth, nativeHeight);

        var config = new SCStreamConfiguration
        {
            Width = (nuint)dimensions.Width,
            Height = (nuint)dimensions.Height,
            ShowsCursor = request.ShowCursor,
            CapturesAudio = request.IncludeSystemAudio,

            // without this the recording captures its own playback if the app makes any sound,
            // which on a screen recorder is almost always feedback rather than content
            ExcludesCurrentProcessAudio = true,

            // SCStreamConfiguration takes a minimum interval between frames, not a rate
            MinimumFrameInterval = new CoreMedia.CMTime(1, dimensions.FrameRate),

            // the frame pool depth; 5 is Apple's own recommendation for recording and leaves
            // enough slack that a slow encoder drops frames instead of stalling the compositor
            QueueDepth = 5
        };

        if (request.IncludeMicrophone && UseRecordingOutput)
            config.CaptureMicrophone = true;

        this.Logger.CaptureConfigured(dimensions.Width, dimensions.Height, dimensions.FrameRate, dimensions.Bitrate);
        this.Logger.UsingCapturePath(UseRecordingOutput ? "SCRecordingOutput" : "AVAssetWriter");

        var recording = new MacOSScreenRecording(
            request,
            this.Capabilities,
            this.PlatformReason,
            filter,
            config,
            dimensions,
            outputPath!,
            UseRecordingOutput,
            logger
        );

        await recording.Start(ct).ConfigureAwait(false);

        return recording;
    }


    static (SCContentFilter Filter, int Width, int Height) BuildFilter(SCShareableContent content, CaptureTarget? target)
    {
        if (target == null)
        {
            var display = content.Displays.FirstOrDefault(d => d.Frame.X == 0 && d.Frame.Y == 0)
                ?? content.Displays.FirstOrDefault()
                ?? throw new ScreenRecorderException("macOS reported no displays to record");

            return (new SCContentFilter(display, Array.Empty<SCWindow>(), SCContentFilterOption.Exclude), (int)display.Width, (int)display.Height);
        }

        var (kind, id) = ParseId(target.Id);

        if (kind == CaptureTargetKind.Display)
        {
            var display = content.Displays.FirstOrDefault(d => d.DisplayId == id)
                ?? throw new ScreenRecorderException($"Display '{target.Id}' is no longer connected");

            return (new SCContentFilter(display, Array.Empty<SCWindow>(), SCContentFilterOption.Exclude), (int)display.Width, (int)display.Height);
        }

        var window = content.Windows.FirstOrDefault(w => w.WindowId == id)
            ?? throw new ScreenRecorderException($"Window '{target.Id}' is no longer open");

        return (new SCContentFilter(window), (int)window.Frame.Width, (int)window.Frame.Height);
    }


    static async Task<SCShareableContent> GetShareableContent()
    {
        try
        {
            // excludeDesktopWindows: the wallpaper and desktop icons are not things anyone picks;
            // onScreenWindowsOnly: minimised windows cannot be captured at all
            return await SCShareableContent.GetShareableContentAsync(true, true).ConfigureAwait(false);
        }
        catch (NSErrorException ex)
        {
            throw new ScreenRecorderPermissionException(
                "macOS refused to list shareable content, which almost always means the Screen Recording permission is missing. Approve it in System Settings > Privacy & Security > Screen Recording, then relaunch the app",
                ex
            );
        }
    }


    // the id has to round-trip through a string on the public API, and a display id and a window id
    // can collide, so the kind is carried alongside it
    static string FormatId(CaptureTargetKind kind, uint id) => $"{kind}:{id}";

    static (CaptureTargetKind Kind, uint Id) ParseId(string id)
    {
        var parts = id.Split(':', 2);

        if (parts.Length == 2 && Enum.TryParse<CaptureTargetKind>(parts[0], out var kind) && UInt32.TryParse(parts[1], out var value))
            return (kind, value);

        throw new ScreenRecorderException($"'{id}' is not a capture target id produced by GetTargets");
    }
}

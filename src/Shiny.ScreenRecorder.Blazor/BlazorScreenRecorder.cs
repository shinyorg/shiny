using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Shiny.ScreenRecorder;


/// <summary>
/// The Blazor WebAssembly implementation, backed by <c>getDisplayMedia</c> and
/// <c>MediaRecorder</c>.
/// </summary>
/// <remarks>
/// <para>The only platform here where pause is native rather than synthesised, and the only one
/// where the output is not a file - the browser has no filesystem, so
/// <see cref="ScreenRecordingResult.FilePath"/> is null and the recording is read back through
/// <see cref="ScreenRecordingResult.OpenRead"/> or handed to the user with
/// <see cref="BlazorScreenRecorderExtensions.DownloadRecording"/>.</para>
/// <para>The browser owns target selection: <c>getDisplayMedia</c> shows its own picker covering
/// screens, windows and tabs, so <see cref="GetTargets"/> throws and
/// <see cref="ScreenRecordingRequest.Target"/> must be null.</para>
/// <para><b>The container varies by browser and the result says which.</b> Safari and recent
/// Chrome produce MP4/H.264; Firefox produces WebM/VP9. Do not assume MP4 when uploading.</para>
/// <para><c>getDisplayMedia</c> requires a secure context (HTTPS or localhost) and must be called
/// from a user gesture - a button click. Calling <see cref="IScreenRecorder.Start"/> from
/// <c>OnInitializedAsync</c> will be refused by the browser.</para>
/// </remarks>
public class BlazorScreenRecorder(IJSRuntime jsRuntime, ILogger<BlazorScreenRecorder> logger) : AbstractScreenRecorder(logger)
{
    IJSObjectReference? module;
    BrowserProbe? probe;


    internal async Task<IJSObjectReference> GetModule()
    {
        this.module ??= await jsRuntime
            .InvokeAsync<IJSObjectReference>("import", "./_content/Shiny.ScreenRecorder.Blazor/screen-recorder.js")
            .ConfigureAwait(false);

        return this.module;
    }


    /// <summary>
    /// Reads what this browser supports. Must be awaited before <see cref="Capabilities"/> reports
    /// anything but <see cref="ScreenRecorderCapabilities.None"/>.
    /// </summary>
    /// <remarks>
    /// Feature detection needs a JS round trip, and <see cref="Capabilities"/> is synchronous by
    /// contract. Rather than block or lie, the probe is cached the first time anything asynchronous
    /// runs - <see cref="RequestAccess"/> or <see cref="IScreenRecorder.Start"/> - and callers that
    /// want the flags up front can call this directly.
    /// </remarks>
    public async Task<ScreenRecorderCapabilities> Probe(CancellationToken ct = default)
    {
        var module = await this.GetModule().ConfigureAwait(false);
        this.probe = await module.InvokeAsync<BrowserProbe>("probe", ct).ConfigureAwait(false);

        return this.Capabilities;
    }


    public override ScreenRecorderCapabilities Capabilities
    {
        get
        {
            if (this.probe is not { Supported: true })
                return ScreenRecorderCapabilities.None;

            var caps = ScreenRecorderCapabilities.Recording
                | ScreenRecorderCapabilities.PauseResume
                | ScreenRecorderCapabilities.FrameRateControl
                | ScreenRecorderCapabilities.BitrateControl
                | ScreenRecorderCapabilities.Downscaling

                // the browser picker always draws the cursor and offers no say in it, so the flag
                // is present only in the sense that ShowCursor's default is honoured - setting it
                // false is refused rather than silently ignored
                ;

            if (this.probe.HasMicrophone)
                caps |= ScreenRecorderCapabilities.Microphone;

            if (this.probe.HasSystemAudio)
                caps |= ScreenRecorderCapabilities.SystemAudio;

            return caps;
        }
    }


    protected override string PlatformReason =>
        "the browser runs its own screen picker and does not disclose a list of displays or windows, always draws the cursor, and only Chromium offers the audio of the surface being shared";


    public override async Task<AccessState> RequestAccess(ScreenRecordingRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (this.probe == null)
            await this.Probe(ct).ConfigureAwait(false);

        if (this.probe is not { Supported: true })
            return AccessState.NotSupported;

        // getDisplayMedia grants per-call from inside the picker and cannot be pre-approved, so
        // there is nothing to ask for ahead of time
        return AccessState.Unknown;
    }


    protected override string? ResolveOutputPath(ScreenRecordingRequest request) => null;


    protected override async Task<AbstractScreenRecording> OnStart(
        ScreenRecordingRequest request,
        string? outputPath,
        CancellationToken ct
    )
    {
        if (this.probe == null)
            await this.Probe(ct).ConfigureAwait(false);

        if (this.probe is not { Supported: true })
            throw ScreenRecorderNotSupportedException.For(
                ScreenRecorderCapabilities.Recording,
                "this browser has neither getDisplayMedia nor a MediaRecorder container this library can produce"
            );

        var module = await this.GetModule().ConfigureAwait(false);
        var recording = new BlazorScreenRecording(request, this.Capabilities, this.PlatformReason, module, logger);

        await recording.Start(ct).ConfigureAwait(false);

        return recording;
    }


    /// <summary>What the browser reported it can do.</summary>
    internal sealed record BrowserProbe(bool Supported, string MimeType, bool HasMicrophone, bool HasSystemAudio);
}

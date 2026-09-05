using AVFoundation;
using Microsoft.Extensions.Logging;
using ReplayKit;

namespace Shiny.ScreenRecorder;


/// <summary>
/// The iOS and Mac Catalyst implementation, backed by ReplayKit.
/// </summary>
/// <remarks>
/// <para><b>This records your own app's UI, and nothing else.</b> ReplayKit will record the whole
/// system, but only from inside a Broadcast Upload Extension - a second app target that a NuGet
/// package cannot deliver. Everything here uses <c>RPScreenRecorder.startCapture</c>, which is the
/// in-app path.</para>
/// <para>It uses <c>startCapture</c> rather than <c>startRecording</c> deliberately.
/// <c>startRecording</c> keeps the movie inside ReplayKit and will only surrender it through
/// <c>RPPreviewViewController</c>, which is a user-facing share sheet - no use to a library that
/// promises a file path. <c>startCapture</c> hands over CMSampleBuffers instead, which go into
/// <see cref="AssetWriterSink"/>.</para>
/// <para>Two consequences worth designing around: the app must be in the foreground for capture to
/// continue, and ReplayKit stops the recording itself on an incoming call or when the screen
/// locks. Both surface as <see cref="IScreenRecording.Faulted"/> rather than as a silent
/// truncation.</para>
/// </remarks>
public class AppleScreenRecorder(ILogger<AppleScreenRecorder> logger) : AbstractScreenRecorder(logger)
{
    public override ScreenRecorderCapabilities Capabilities
    {
        get
        {
            if (!RPScreenRecorder.SharedRecorder.Available)
                return ScreenRecorderCapabilities.None;

            // no cursor to draw, no display or window to choose, and ReplayKit offers no say over
            // the capture frame rate - it delivers what the compositor produces. Bitrate and
            // downscaling are ours because we own the AVAssetWriter.
            var caps = ScreenRecorderCapabilities.Recording
                | ScreenRecorderCapabilities.PauseResume
                | ScreenRecorderCapabilities.SystemAudio
                | ScreenRecorderCapabilities.BitrateControl
                | ScreenRecorderCapabilities.Downscaling;

#if !TVOS
            // there is no microphone on an Apple TV, and RPScreenRecorder carries no
            // MicrophoneEnabled on tvOS to ask for one
            caps |= ScreenRecorderCapabilities.Microphone;
#endif
            return caps;
        }
    }


    protected override string PlatformReason =>
        "ReplayKit records the app's own UI in-app - it exposes no display or window list, no cursor, and no control over the capture frame rate";


    public override async Task<AccessState> RequestAccess(ScreenRecordingRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Available goes false while a call is active, during Guided Access, and on some managed
        // devices - it is a runtime state, not a static capability
        if (!RPScreenRecorder.SharedRecorder.Available)
            return AccessState.Disabled;

        if (!request.IncludeMicrophone)
            return AccessState.Available;

#if TVOS
        // Microphone is never advertised on tvOS, so a request that asks for it has already been
        // rejected by validation - this is here only to keep the branch honest
        return AccessState.NotSupported;
#else
        var granted = await AVCaptureDevice
            .RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Audio)
            .ConfigureAwait(false);

        if (!granted)
            this.Logger.PermissionRefused("Microphone access was denied. Add NSMicrophoneUsageDescription to Info.plist and approve the prompt");

        return granted ? AccessState.Available : AccessState.Denied;
#endif
    }


    protected override async Task<AbstractScreenRecording> OnStart(
        ScreenRecordingRequest request,
        string? outputPath,
        CancellationToken ct
    )
    {
        if (!RPScreenRecorder.SharedRecorder.Available)
            throw new ScreenRecorderException("ReplayKit is not available right now - a call, Guided Access or a device policy can all take it away temporarily");

        var recording = new AppleScreenRecording(
            request,
            this.Capabilities,
            this.PlatformReason,
            outputPath!,
            logger
        );

        await recording.Start(ct).ConfigureAwait(false);

        return recording;
    }
}

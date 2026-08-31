using Android;
using Android.Content;
using Android.Media.Projection;
using Android.Util;
using Android.Views;
using Microsoft.Extensions.Logging;
using Shiny.ScreenRecorder.Infrastructure;

namespace Shiny.ScreenRecorder;


/// <summary>
/// The Android implementation, backed by MediaProjection.
/// </summary>
/// <remarks>
/// <para>Starting a recording here is a three-step dance and the order is fixed by the OS:
/// consent through <see cref="ScreenCapturePermissionActivity"/>, then
/// <see cref="ScreenRecorderService"/> in the foreground, and only then
/// <c>getMediaProjection</c>. From Android 14 the projection call throws outright if the
/// foreground service is not already running.</para>
/// <para>The encoder is MediaCodec + MediaMuxer rather than MediaRecorder, which would be a
/// fraction of the code. MediaRecorder accepts a single audio source and playback capture is not
/// one of them, so app audio - a capability this module claims on every other platform that has it
/// - is only reachable through <c>AudioRecord</c> and therefore through MediaCodec. Doing it once
/// this way is better than two divergent code paths.</para>
/// <para>Consent is per-recording and cannot be pre-granted, so
/// <see cref="RequestAccess"/> only answers for the microphone; the screen itself reports
/// <see cref="AccessState.Unknown"/> and the real prompt appears inside <see cref="IScreenRecorder.Start"/>.</para>
/// </remarks>
public class AndroidScreenRecorder(AndroidPlatform platform, ILogger<AndroidScreenRecorder> logger) : AbstractScreenRecorder(logger)
{
    public override ScreenRecorderCapabilities Capabilities
    {
        get
        {
            var caps = ScreenRecorderCapabilities.Recording
                | ScreenRecorderCapabilities.PauseResume
                | ScreenRecorderCapabilities.Microphone
                | ScreenRecorderCapabilities.FrameRateControl
                | ScreenRecorderCapabilities.BitrateControl
                | ScreenRecorderCapabilities.Downscaling;

            // AudioPlaybackCaptureConfiguration is API 29; below that there is no way for an app to
            // record what another app is playing
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
                caps |= ScreenRecorderCapabilities.SystemAudio;

            return caps;
        }
    }


    protected override string PlatformReason =>
        "MediaProjection captures the whole screen as the compositor draws it - there is no display or window list to choose from, and no cursor layer to toggle";


    // the app's own cache, which is the only place a MediaMuxer can reliably write without
    // storage permissions on any API level
    protected override string DefaultOutputDirectory => Path.Combine(platform.Cache.FullName, "screen-recordings");


    public override async Task<AccessState> RequestAccess(ScreenRecordingRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var permissions = new List<string>();

        if (request.IncludeMicrophone || request.IncludeSystemAudio)
            permissions.Add(Manifest.Permission.RecordAudio);

        if (permissions.Count > 0)
        {
            var result = await platform
                .RequestPermissions(ct, permissions.ToArray())
                .ConfigureAwait(false);

            if (!result.IsSuccess())
            {
                this.Logger.PermissionRefused("RECORD_AUDIO was denied - the recording will have no audio");
                return AccessState.Denied;
            }
        }

        var foreground = await platform.RequestForegroundServicePermissions().ConfigureAwait(false);
        if (foreground != AccessState.Available)
            return foreground;

        // the screen capture token itself is granted per-recording through a dialog that cannot be
        // shown ahead of time, so this is genuinely unknown until Start runs
        return AccessState.Unknown;
    }


    protected override async Task<AbstractScreenRecording> OnStart(
        ScreenRecordingRequest request,
        string? outputPath,
        CancellationToken ct
    )
    {
        var consent = await ScreenCapturePermissionActivity.Request(platform, ct).ConfigureAwait(false);
        if (!consent.Granted || consent.Data == null)
        {
            this.Logger.ConsentDeclined();
            throw new ScreenRecorderPermissionException("The user declined the screen capture prompt");
        }

        // Android 14+ requires a mediaProjection foreground service to already be running before
        // getMediaProjection is called - not after
        await ScreenRecorderService.StartAndWait(platform, ct).ConfigureAwait(false);

        var manager = (MediaProjectionManager?)platform.AppContext.GetSystemService(Context.MediaProjectionService)
            ?? throw new ScreenRecorderException("This device has no MediaProjection service");

        MediaProjection projection;
        try
        {
            projection = manager.GetMediaProjection(consent.ResultCode, consent.Data)
                ?? throw new ScreenRecorderException("MediaProjection was not granted");
        }
        catch (Java.Lang.SecurityException ex)
        {
            ScreenRecorderService.StopService(platform);
            throw new ScreenRecorderPermissionException(
                "Android refused the media projection. On Android 14+ this means the foreground service declaration is missing - add FOREGROUND_SERVICE_MEDIA_PROJECTION to the manifest",
                ex
            );
        }

        var metrics = this.GetScreenMetrics();
        var dimensions = VideoDimensions.From(request, metrics.Width, metrics.Height);
        this.Logger.CaptureConfigured(dimensions.Width, dimensions.Height, dimensions.FrameRate, dimensions.Bitrate);

        var recording = new AndroidScreenRecording(
            request,
            this.Capabilities,
            this.PlatformReason,
            platform,
            projection,
            dimensions,
            metrics.DensityDpi,
            outputPath!,
            logger
        );

        try
        {
            recording.Start();
        }
        catch
        {
            await recording.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        return recording;
    }


    /// <summary>
    /// The real size of the display being captured.
    /// </summary>
    /// <remarks>
    /// Reads <c>WindowMetrics</c> off the current activity where there is one - that is the only
    /// API that reports the full display including the area behind the system bars, and it needs a
    /// visual context, which the application context is not. Falls back to the resource display
    /// metrics, which under-report by the height of the bars but are always available.
    /// </remarks>
    (int Width, int Height, int DensityDpi) GetScreenMetrics()
    {
        var display = platform.AppContext.Resources?.DisplayMetrics
            ?? throw new ScreenRecorderException("Android reported no display metrics");

        var dpi = (int)display.DensityDpi;
        var activity = platform.CurrentActivity;

        if (activity != null && OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var bounds = activity.WindowManager?.CurrentWindowMetrics?.Bounds;

            if (bounds != null && bounds.Width() > 0 && bounds.Height() > 0)
                return (bounds.Width(), bounds.Height(), dpi);
        }

        return (display.WidthPixels, display.HeightPixels, dpi);
    }
}

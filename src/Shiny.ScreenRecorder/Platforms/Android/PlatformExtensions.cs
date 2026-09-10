using Android.App;

namespace Shiny.ScreenRecorder;


public static class PlatformExtensions
{
    /// <summary>
    /// The intent behind the recording notification's Stop action, for an
    /// <see cref="IScreenRecordingNotificationDelegate"/> that relabels it.
    /// </summary>
    /// <remarks>
    /// The recording ends the way the system's own stop control ends it:
    /// <see cref="IScreenRecording.Faulted"/> fires with <see cref="ScreenRecordingFaultReason.RevokedByUser"/>
    /// and carries the finished file.
    /// </remarks>
    public static PendingIntent GetScreenRecordingStopIntent(this AndroidPlatform platform) => PendingIntent.GetService(
        platform.AppContext,
        0,
        platform.CreateIntent<ScreenRecorderService>(ScreenRecorderService.ActionStopRecording),

        // immutable on purpose - there is nothing to fill in, and GetPendingIntentFlags would force Mutable
        PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
    )!;
}

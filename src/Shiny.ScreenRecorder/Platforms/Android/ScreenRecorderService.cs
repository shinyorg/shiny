using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Extensions.Logging;

namespace Shiny.ScreenRecorder;


/// <summary>
/// The foreground service that has to be running before a MediaProjection can be obtained.
/// </summary>
/// <remarks>
/// <para><b>The ordering is not optional on Android 14+.</b> From API 34,
/// <c>MediaProjectionManager.getMediaProjection</c> throws <c>SecurityException</c> unless a
/// foreground service of type <c>mediaProjection</c> is *already* running. So the sequence is
/// consent, then this service, then the projection - not the more natural projection-then-service.</para>
/// <para>The service does no work itself. It exists to hold the process in the foreground for the
/// life of the recording, and it stops as soon as the recording does.</para>
/// <para>The persistent notification is not suppressible, and neither is the system's own screen
/// cast indicator. That is deliberate on Android's part and this package does not try to work
/// around it.</para>
/// </remarks>
[Android.App.Service(
    Enabled = true,
    Exported = false,
    ForegroundServiceType = ForegroundService.TypeMediaProjection
)]
public class ScreenRecorderService : ShinyAndroidForegroundService
{
    static TaskCompletionSource? ready;

    public static bool IsStarted { get; private set; }

    // the mediaProjection foreground service type only exists from API 29; below that MediaProjection
    // needs no typed service at all, so an untyped one is both correct and the only legal option
    protected override ForegroundService StartForegroundServiceType
        => OperatingSystem.IsAndroidVersionAtLeast(29)
            ? ForegroundService.TypeMediaProjection
            : base.StartForegroundServiceType;


    /// <summary>
    /// Starts the service and does not return until it is genuinely in the foreground.
    /// </summary>
    /// <remarks>
    /// Awaiting the promotion rather than assuming it is the whole point - calling
    /// <c>getMediaProjection</c> a moment too early is the failure this exists to prevent.
    /// </remarks>
    internal static async Task StartAndWait(AndroidPlatform platform, CancellationToken ct)
    {
        if (IsStarted)
            return;

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Interlocked.Exchange(ref ready, tcs);

        platform.StartService(typeof(ScreenRecorderService), stopWithTask: true);

        // a service that never reaches the foreground would otherwise hang the start forever; five
        // seconds is Android's own ANR budget for startForeground, so exceeding it means it failed
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));

        try
        {
            await tcs.Task.WaitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new ScreenRecorderException("The screen recording foreground service did not start. Check that FOREGROUND_SERVICE and FOREGROUND_SERVICE_MEDIA_PROJECTION are in the manifest");
        }
    }


    internal static void StopService(AndroidPlatform platform)
    {
        if (IsStarted)
            platform.StopService(typeof(ScreenRecorderService));
    }


    protected override void OnStart(Intent? intent)
    {
        IsStarted = true;
        Interlocked.Exchange(ref ready, null)?.TrySetResult();
    }


    protected override void OnStop()
    {
        IsStarted = false;
        Interlocked.Exchange(ref ready, null)?.TrySetCanceled();
    }


    /// <summary>
    /// Android 15 caps how long a foreground service may run. A mediaProjection service that
    /// overruns is killed, so it stops itself promptly and the recording faults rather than the
    /// process being taken down.
    /// </summary>
    public override void OnTimeout(int startId)
    {
        this.Logger.LogWarning("The screen recording foreground service hit its time limit - the recording will be stopped");
        this.Stop();
    }


    /// <inheritdoc cref="OnTimeout(int)"/>
    public override void OnTimeout(int startId, ForegroundService fgsType) => this.OnTimeout(startId);
}

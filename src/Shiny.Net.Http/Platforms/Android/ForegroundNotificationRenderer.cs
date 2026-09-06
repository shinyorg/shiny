using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Runtime;
using AndroidX.Core.App;
using Microsoft.Extensions.Logging;

namespace Shiny.Net.Http;


/// <summary>
/// Draws transfer progress onto the foreground-service notification the transfer service already has to
/// post, upgrading it to an Android 16 "live update" where the OS supports one.
/// </summary>
/// <remarks>
/// Android requires a foreground service to move bytes in the background, and a foreground service requires
/// a notification. Posting a <em>second</em> notification to show progress - which is what the older
/// <c>PerTransferNotificationStrategy</c> did - leaves the user with two entries in the shade, one of them
/// the useless "Shiny service is continuing to transfer data in the background". This renderer re-posts the
/// service's own notification id instead, so there is exactly one.
/// <para>
/// On API 36+ it uses <c>Notification.ProgressStyle</c> with <c>requestPromotedOngoing</c> and
/// <c>setShortCriticalText</c>, which gets the status bar chip and always-on-display treatment - the
/// closest Android analogue to an iOS Live Activity. Below 36 it degrades to an ordinary determinate
/// progress bar.
/// </para>
/// </remarks>
public class ForegroundNotificationRenderer(
    AndroidPlatform platform,
    ILogger<ForegroundNotificationRenderer> logger
) : ITransferProgressRenderer
{
    string? currentKey;


    /// <summary>
    /// True from API 26. Below that there are no notification channels and no foreground-service
    /// notification worth updating.
    /// </summary>
    public bool IsAvailable => OperatingSystem.IsAndroidVersionAtLeast(26);


    /// <inheritdoc />
    public Task Show(string key, TransferProgressContent content)
    {
        // There is only ever one foreground-service notification. With the default Summary scope exactly
        // one key ever arrives; with PerTransfer, the most recently updated transfer wins the notification.
        this.currentKey = key;
        this.Post(content, ongoing: true);
        return Task.CompletedTask;
    }


    /// <inheritdoc />
    public Task Hide(string key, TransferProgressContent content, DateTimeOffset dismissAt)
    {
        if (this.currentKey != key)
            return Task.CompletedTask;

        // The service stops itself when the queue drains, and StopForeground removes the notification with
        // it - so the final state is drawn and then simply goes away. Nothing to schedule.
        this.Post(content, ongoing: false);
        this.currentKey = null;
        return Task.CompletedTask;
    }


    /// <inheritdoc />
    public Task Reconcile(IReadOnlyCollection<string> activeKeys)
    {
        // Nothing to do: the notification belongs to the service, so a process that died took it with it.
        this.currentKey = null;
        return Task.CompletedTask;
    }


    void Post(TransferProgressContent content, bool ongoing)
    {
        var service = HttpTransferService.Current;
        if (service == null)
        {
            logger.LogDebug("The transfer foreground service is not running - nothing to draw progress on");
            return;
        }

        try
        {
            var notification = this.CreateBuilder(content, ongoing).Build();
            notification.Flags |= NotificationFlags.ForegroundService;

            NotificationManagerCompat
                .From(platform.AppContext)
                .Notify(service.ForegroundNotificationId, notification);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update the transfer foreground notification");
        }
    }


    /// <summary>
    /// Builds the native notification. Override to restyle it - actions, icons, custom layouts - while
    /// keeping the live-update handling here.
    /// </summary>
    /// <param name="content">The content being drawn.</param>
    /// <param name="ongoing">Whether transfers are still running.</param>
    protected virtual Notification.Builder CreateBuilder(TransferProgressContent content, bool ongoing)
    {
        // The service's own channel, so re-posting its id keeps the notification where the user expects it.
        var builder = new Notification.Builder(platform.AppContext, ShinyAndroidForegroundService.NotificationChannelId)
            .SetSmallIcon(platform.GetNotificationIconResource())
            .SetContentTitle(content.Title)
            .SetContentText(content.Body)
            .SetOngoing(ongoing)
            .SetOnlyAlertOnce(content.Alert == null)!;

        if (platform.AppContext.PackageName is { } packageName)
        {
            var launch = platform.AppContext.PackageManager?.GetLaunchIntentForPackage(packageName);
            if (launch != null)
            {
                builder.SetContentIntent(PendingIntent.GetActivity(
                    platform.AppContext,
                    0,
                    launch,
                    PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
                ));
            }
        }

        this.ApplyProgress(builder, content, ongoing);
        return builder;
    }


    void ApplyProgress(Notification.Builder builder, TransferProgressContent content, bool ongoing)
    {
        var progress = content.Progress;

        if (OperatingSystem.IsAndroidVersionAtLeast(36))
        {
            // Android 16 Live Updates: the status bar chip and always-on display treatment.
            if (ongoing)
                TryRequestPromotedOngoing(builder, logger);

            if (content.ShortStatus is { } shortStatus)
                builder.SetShortCriticalText(shortStatus);

            if (progress is not null)
            {
                var style = new Notification.ProgressStyle();
                style.SetProgress(ToPercent(progress));
                style.SetProgressIndeterminate(progress.Indeterminate);
                builder.SetStyle(style);
            }
        }
        else if (progress is not null)
        {
            builder.SetProgress(100, ToPercent(progress), progress.Indeterminate);
        }
    }


    // Android cannot animate a time range the way ActivityKit does, so a projected range is resolved back
    // to a fraction here. It costs nothing: the foreground service is alive for the whole transfer, so
    // real progress callbacks keep arriving and the bar never has to coast.
    static int ToPercent(TransferProgressBar progress)
        => (int)Math.Round((progress.ToFraction() ?? 0d) * 100d);


    // .NET for Android 36 does not bind Notification.Builder.requestPromotedOngoing yet, so it is invoked
    // directly. Without it the notification still posts - it just doesn't get the status bar chip.
    static void TryRequestPromotedOngoing(Notification.Builder builder, ILogger logger)
    {
        try
        {
            var cls = JNIEnv.GetObjectClass(builder.Handle);
            var method = JNIEnv.GetMethodID(cls, "requestPromotedOngoing", "(Z)Landroid/app/Notification$Builder;");
            if (method != IntPtr.Zero)
                JNIEnv.CallObjectMethod(builder.Handle, method, new JValue(true));
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "requestPromotedOngoing is unavailable - progress will post without the status bar chip");
        }
    }
}

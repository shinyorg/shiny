namespace Shiny.DeviceTests.Tests;


/// <summary>
/// Shiny has no "delivered" callback for local notifications, so the suites read the OS's own list of what is
/// currently showing - UNUserNotificationCenter on Apple, NotificationManager.GetActiveNotifications on Android.
/// </summary>
public static class DeliveredNotifications
{
    public record Delivered(int Id, string? ChannelId);


    public static async Task<IReadOnlyList<Delivered>> GetAsync()
    {
#if IOS || MACCATALYST
        var delivered = await UserNotifications.UNUserNotificationCenter.Current
            .GetDeliveredNotificationsAsync()
            .ConfigureAwait(false);

        return delivered
            .Select(x => Int32.TryParse(x.Request.Identifier, out var id) ? new Delivered(id, null) : null)
            .OfType<Delivered>()
            .ToList();
#elif ANDROID
        await Task.CompletedTask.ConfigureAwait(false);
        var manager = (Android.App.NotificationManager)Android.App.Application.Context
            .GetSystemService(Android.Content.Context.NotificationService)!;

        return (manager.GetActiveNotifications() ?? [])
            .Select(x => new Delivered(x.Id, x.Notification?.ChannelId))
            .ToList();
#else
        await Task.CompletedTask.ConfigureAwait(false);
        return [];
#endif
    }


    public static async Task<bool> IsDeliveredAsync(int id)
        => (await GetAsync().ConfigureAwait(false)).Any(x => x.Id == id);


    public static async Task WaitForDeliveryAsync(TestContext ctx, int id, TimeSpan timeout)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ctx.CancellationToken);
        cts.CancelAfter(timeout);
        try
        {
            while (!await IsDeliveredAsync(id).ConfigureAwait(false))
                await Task.Delay(500, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ctx.CancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Notification {id} was not delivered within {timeout.TotalSeconds:0}s");
        }
    }


    public static async Task WaitForRemovalAsync(TestContext ctx, int id, TimeSpan timeout)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ctx.CancellationToken);
        cts.CancelAfter(timeout);
        try
        {
            while (await IsDeliveredAsync(id).ConfigureAwait(false))
                await Task.Delay(250, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ctx.CancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Notification {id} was still showing after {timeout.TotalSeconds:0}s");
        }
    }


    /// <summary>
    /// Whether the delivered notification carries an image - an attachment on Apple, a BigPictureStyle picture on
    /// Android. Null when it isn't showing.
    /// </summary>
    public static async Task<bool?> HasImageAsync(int id)
    {
#if IOS || MACCATALYST
        var delivered = await UserNotifications.UNUserNotificationCenter.Current
            .GetDeliveredNotificationsAsync()
            .ConfigureAwait(false);

        var match = delivered.FirstOrDefault(x => x.Request.Identifier == id.ToString());
        return match == null ? null : match.Request.Content.Attachments.Length > 0;
#elif ANDROID
        await Task.CompletedTask.ConfigureAwait(false);
        var manager = (Android.App.NotificationManager)Android.App.Application.Context
            .GetSystemService(Android.Content.Context.NotificationService)!;

        var match = (manager.GetActiveNotifications() ?? []).FirstOrDefault(x => x.Id == id);
        return match == null ? null : match.Notification?.Extras?.ContainsKey(AndroidX.Core.App.NotificationCompat.ExtraPicture) == true;
#else
        await Task.CompletedTask.ConfigureAwait(false);
        return null;
#endif
    }


#if IOS || MACCATALYST
    /// <summary>
    /// Waits for a delivered notification whose payload carries <paramref name="key"/> = <paramref name="value"/> - a
    /// push, whose request identifier is a UUID iOS picks
    /// </summary>
    public static async Task<UserNotifications.UNNotification> WaitForPayloadAsync(TestContext ctx, string key, string value, TimeSpan timeout)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ctx.CancellationToken);
        cts.CancelAfter(timeout);
        try
        {
            while (true)
            {
                var delivered = await UserNotifications.UNUserNotificationCenter.Current
                    .GetDeliveredNotificationsAsync()
                    .ConfigureAwait(false);

                var match = delivered.FirstOrDefault(x => x.Request.Content.UserInfo?[key]?.ToString() == value);
                if (match != null)
                    return match;

                await Task.Delay(250, cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!ctx.CancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"No notification with {key}={value} was delivered within {timeout.TotalSeconds:0}s");
        }
    }
#endif
}

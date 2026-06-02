using Microsoft.Extensions.Logging;
using Sample.Shared.Maui.Services;
using Shiny.Push;

namespace Sample.Shared.Maui.Delegates;

public class SamplePushDelegate(
    ILogger<SamplePushDelegate> logger,
    IEventStore events
) : IPushDelegate
{
    public Task OnEntry(PushNotification notification)
    {
        logger.LogInformation("Push Entry");
        return events.Add("Push", "Entry", BuildMetadata(notification));
    }

    public Task OnReceived(PushNotification notification)
    {
        logger.LogInformation("Push Received");
        return events.Add("Push", "Received", BuildMetadata(notification));
    }

    public Task OnNewToken(string token)
    {
        logger.LogInformation("Push New Token: {Token}", token);
        return events.Add(
            "Push",
            "New Token",
            new Dictionary<string, string?> { ["Token"] = token }
        );
    }

    public Task OnUnRegistered(string token)
    {
        logger.LogInformation("Push UnRegistered: {Token}", token);
        return events.Add(
            "Push",
            "UnRegistered",
            new Dictionary<string, string?> { ["Token"] = token }
        );
    }

    static Dictionary<string, string?> BuildMetadata(PushNotification notification)
    {
        var dict = new Dictionary<string, string?>
        {
            ["Title"] = notification.Notification?.Title,
            ["Message"] = notification.Notification?.Message
        };
        if (notification.Data is { Count: > 0 })
        {
            foreach (var kvp in notification.Data)
                dict[$"data.{kvp.Key}"] = kvp.Value;
        }
        return dict;
    }
}

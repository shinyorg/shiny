using Microsoft.Extensions.Logging;
using Shiny.Push;

namespace Sample.MacOS.Delegates;

public class SamplePushDelegate(ILogger<SamplePushDelegate> logger) : IPushDelegate
{
    public Task OnEntry(PushNotification notification)
    {
        logger.LogInformation("Push OnEntry: {Title}", notification.Notification?.Title);
        return Task.CompletedTask;
    }

    public Task OnReceived(PushNotification notification)
    {
        logger.LogInformation("Push OnReceived: {Title}", notification.Notification?.Title);
        return Task.CompletedTask;
    }

    public Task OnNewToken(string token)
    {
        logger.LogInformation("Push OnNewToken: {Token}", token);
        return Task.CompletedTask;
    }

    public Task OnUnRegistered(string token)
    {
        logger.LogInformation("Push OnUnRegistered: {Token}", token);
        return Task.CompletedTask;
    }
}

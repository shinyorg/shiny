using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Push;

namespace Sample.Blazor;


public class SamplePushDelegate(ILogger<SamplePushDelegate> logger) : IPushDelegate
{
    public Task OnEntry(PushNotification notification)
    {
        logger.LogInformation("Push Entry: {Title}", notification.Notification?.Title);
        return Task.CompletedTask;
    }

    public Task OnReceived(PushNotification notification)
    {
        logger.LogInformation("Push Received: {Title}", notification.Notification?.Title);
        return Task.CompletedTask;
    }

    public Task OnNewToken(string token)
    {
        logger.LogInformation("New push token: {Token}", token);
        return Task.CompletedTask;
    }

    public Task OnUnRegistered(string token)
    {
        logger.LogInformation("Push unregistered: {Token}", token);
        return Task.CompletedTask;
    }
}

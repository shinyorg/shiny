using Microsoft.Extensions.Logging;
using Shiny.Push;

namespace Sample.Shared.Maui.Delegates;

public class SamplePushDelegate(ILogger<SamplePushDelegate> logger) : IPushDelegate
{
    public Task OnEntry(PushNotification notification)
    {
        logger.LogInformation("Push Entry");
        return Task.CompletedTask;
    }

    public Task OnReceived(PushNotification notification)
    {
        logger.LogInformation("Push Received");
        return Task.CompletedTask;
    }

    public Task OnNewToken(string token)
    {
        logger.LogInformation("Push New Token: {Token}", token);
        return Task.CompletedTask;
    }

    public Task OnUnRegistered(string token)
    {
        logger.LogInformation("Push UnRegistered: {Token}", token);
        return Task.CompletedTask;
    }
}

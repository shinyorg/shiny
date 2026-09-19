using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Wearables;


static class WearableDelegates
{
    /// <summary>
    /// Asks each delegate in turn for a reply to a message; the first non-null one wins. A delegate that throws is
    /// logged and skipped, so one broken delegate does not leave the wearable without an answer.
    /// </summary>
    public static async Task<byte[]> GetReply(IEnumerable<IWearableDelegate> delegates, WearableMessage message, ILogger logger)
    {
        foreach (var d in delegates)
        {
            try
            {
                var reply = await d.OnMessageReceived(message).ConfigureAwait(false);
                if (reply != null)
                    return reply;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Wearable delegate {Delegate} failed on message {Path}", d.GetType().FullName, message.Path);
            }
        }
        return [];
    }


    public static Task<byte[]> GetReply(IServiceProvider services, WearableMessage message, ILogger logger)
        => GetReply(services.GetServices<IWearableDelegate>(), message, logger);
}

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Notifications.Infrastructure;
using Shiny.Stores;

namespace Shiny.Notifications;


public static class CommonExtensions
{
    internal static void AssertChannelRemove(this IChannelManager channelManager, string channelIdentifier)
    {
        if (channelIdentifier == null)
            throw new ArgumentNullException(nameof(channelIdentifier));

        if (channelIdentifier.Equals(Channel.Default.Identifier))
            throw new InvalidOperationException("You cannot remove the default channel");
    }


    public static IServiceCollection AddChannelManager(this IServiceCollection services)
    {
#if IOS || MACCATALYST || ANDROID
        services.AddRepository<ChannelStoreConverter, Channel>();
        services.TryAddSingleton<IChannelManager, ChannelManager>();
#endif
        return services;
    }
}

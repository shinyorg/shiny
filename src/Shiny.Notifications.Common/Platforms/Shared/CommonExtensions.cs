using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Hosting;
using Shiny.Notifications.Infrastructure;

namespace Shiny.Notifications;


public static class CommonExtensions
{
    //public static void SetSoundFromEmbeddedResource(this Channel channel, Assembly assembly, string resourceName)
    //    => channel.CustomSoundPath = Host
    //        .Current
    //        .Services
    //        .GetRequiredService<IPlatform>()
    //        .ResourceToFilePath(assembly, resourceName);


    internal static void AssertChannelRemove(this IChannelManager channelManager, string channelIdentifier)
    {
        if (channelIdentifier == null)
            throw new ArgumentNullException(nameof(channelIdentifier));

        if (channelIdentifier.Equals(Channel.Default.Identifier))
            throw new InvalidOperationException("You cannot remove the default channel");
    }


    public static IServiceCollection AddChannelManager(this IServiceCollection services)
    {
        if (!services.HasService<IChannelManager>())
        {
            services.AddRepository<ChannelRepositoryConverter, Channel>();
            services.AddShinyService<ChannelManager>();
        }
        return services;
    }
}

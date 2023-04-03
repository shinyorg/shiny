using System;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Notifications;


public static class CommonExtensions
{
    //public static void SetSoundFromEmbeddedResource(this Channel channel, Assembly assembly, string resourceName)
    //    => channel.CustomSoundPath = Host
    //        .Current
    //        .Services
    //        .GetRequiredService<IPlatform>()
    //        .ResourceToFilePath(assembly, resourceName);

#if ANDROID
    public static void Add(this IChannelManager manager, AndroidChannel channel) => manager.Add(channel);
#elif APPLE
    public static void Add(this IChannelManager manager, AppleChannel channel) => manager.Add(channel);
#endif

    internal static void AssertChannelRemove(this IChannelManager channelManager, string channelIdentifier)
    {
        if (channelIdentifier == null)
            throw new ArgumentNullException(nameof(channelIdentifier));

        if (channelIdentifier.Equals(Channel.Default.Identifier))
            throw new InvalidOperationException("You cannot remove the default channel");
    }


    public static IServiceCollection AddChannelManager(this IServiceCollection services)
    {
        services.AddDefaultRepository();
        if (!services.HasService<IChannelManager>())
            services.AddShinyService<ChannelManager>();
        
        return services;
    }


    internal static TNativeChannel TryToNative<TNativeChannel>(this Channel channel) where TNativeChannel : Channel, new()
    {
        if (channel is TNativeChannel native)
            return native;

        return new TNativeChannel
        {
            Identifier = channel.Identifier,
            Importance = channel.Importance,
            Description = channel.Description,
            Sound = channel.Sound,
            CustomSoundPath = channel.CustomSoundPath,
            Actions = channel.Actions
        };
    }
}

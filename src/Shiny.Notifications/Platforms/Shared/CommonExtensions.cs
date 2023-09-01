using System;

namespace Shiny.Notifications;


public static class CommonExtensions
{

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

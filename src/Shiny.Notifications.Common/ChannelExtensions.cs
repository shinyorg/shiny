using System;
using System.Reflection;


namespace Shiny.Notifications
{
    public static class ChannelExtensions
    {
        public static void AssertValid(this Channel channel)
        {
            if (channel.Identifier.IsEmpty())
                throw new ArgumentException("Channel identifier is required", nameof(channel.Identifier));

            if (channel.Actions != null)
            { 
                foreach (var action in channel.Actions)
                    action.AssertValid();
            }
        }


        public static void SetSoundFromEmbeddedResource(this Channel channel, Assembly assembly, string resourceName)
            => channel.CustomSoundPath = ShinyHost
                .Resolve<IPlatform>()
                .ResourceToFilePath(assembly, resourceName);


        public static void AssertValid(this ChannelAction action)
        {
            if (action.Identifier.IsEmpty())
                throw new ArgumentException("ChannelAction Identifier is required", nameof(action.Identifier));

            if (action.Title.IsEmpty())
                throw new ArgumentException("ChannelAction Title is required", nameof(action.Title));
        }
    }
}

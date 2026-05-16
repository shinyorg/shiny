using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Shiny.Notifications;


public class ChannelManager(ILogger<ChannelManager> logger) : IChannelManager
{
    readonly ConcurrentDictionary<string, Channel> channels = new();

    public void ComponentStart()
    {
        try
        {
            this.Add(Channel.Default);
            logger.LogDebug("Linux channel manager initialized");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create default channel");
        }
    }


    public void Add(Channel channel)
    {
        channel.AssertValid();
        this.channels[channel.Identifier] = channel;
    }


    public void Remove(string channelId)
    {
        if (channelId == null)
            throw new ArgumentNullException(nameof(channelId));
        if (channelId.Equals(Channel.Default.Identifier))
            throw new InvalidOperationException("You cannot remove the default channel");

        this.channels.TryRemove(channelId, out _);
    }


    public void Clear()
    {
        this.channels.Clear();
        this.Add(Channel.Default);
    }


    public Channel? Get(string channelId)
        => this.channels.TryGetValue(channelId, out var c) ? c : null;

    public IList<Channel> GetAll() => this.channels.Values.ToList();
}

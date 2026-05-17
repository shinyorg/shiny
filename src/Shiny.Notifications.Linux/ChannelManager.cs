using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Shiny.Notifications;


public class ChannelManager(ILogger<ChannelManager> logger) : IChannelManager
{
    readonly ConcurrentDictionary<string, Channel> channels = new();
    readonly object readyLock = new();
    bool ready;


    void EnsureReady()
    {
        if (this.ready)
            return;

        lock (this.readyLock)
        {
            if (this.ready)
                return;
            this.ready = true;
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
    }


    public void Add(Channel channel)
    {
        this.EnsureReady();
        channel.AssertValid();
        this.channels[channel.Identifier] = channel;
    }


    public void Remove(string channelId)
    {
        this.EnsureReady();
        if (channelId == null)
            throw new ArgumentNullException(nameof(channelId));
        if (channelId.Equals(Channel.Default.Identifier))
            throw new InvalidOperationException("You cannot remove the default channel");

        this.channels.TryRemove(channelId, out _);
    }


    public void Clear()
    {
        this.EnsureReady();
        this.channels.Clear();
        this.Add(Channel.Default);
    }


    public Channel? Get(string channelId)
    {
        this.EnsureReady();
        return this.channels.TryGetValue(channelId, out var c) ? c : null;
    }


    public IList<Channel> GetAll()
    {
        this.EnsureReady();
        return this.channels.Values.ToList();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Shiny.Support.Repositories;

namespace Shiny.Notifications;


public class ChannelManager : IChannelManager
{
    readonly IRepository repository;
    readonly ILogger<ChannelManager> logger;
    readonly object readyLock = new();
    bool ready;


    public ChannelManager(IRepository repository, ILogger<ChannelManager> logger)
    {
        this.repository = repository;
        this.logger = logger;
    }


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
                this.logger.LogDebug("Windows channel manager initialized");
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to create default channel");
            }
        }
    }


    public void Add(Channel channel)
    {
        this.EnsureReady();
        channel.AssertValid();
        this.repository.Set(channel);
    }


    public void Remove(string channelId)
    {
        this.EnsureReady();
        this.AssertChannelRemove(channelId);
        this.repository.Remove<Channel>(channelId);
    }


    public void Clear()
    {
        this.EnsureReady();
        this.repository.Clear<Channel>();
        this.Add(Channel.Default);
    }


    public Channel? Get(string channelId)
    {
        this.EnsureReady();
        return this.repository.Get<Channel>(channelId);
    }


    public IList<Channel> GetAll()
    {
        this.EnsureReady();
        return this.repository.GetAll<Channel>().ToList();
    }
}

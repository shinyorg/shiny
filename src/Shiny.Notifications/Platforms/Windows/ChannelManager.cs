using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Shiny.Support.Repositories;

namespace Shiny.Notifications;


public class ChannelManager : IChannelManager, IShinyComponentStartup
{
    readonly IRepository repository;
    readonly ILogger<ChannelManager> logger;


    public ChannelManager(IRepository repository, ILogger<ChannelManager> logger)
    {
        this.repository = repository;
        this.logger = logger;
    }


    public void ComponentStart()
    {
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


    public void Add(Channel channel)
    {
        channel.AssertValid();
        this.repository.Set(channel);
    }


    public void Remove(string channelId)
    {
        this.AssertChannelRemove(channelId);
        this.repository.Remove<Channel>(channelId);
    }


    public void Clear()
    {
        this.repository.Clear<Channel>();
        this.Add(Channel.Default);
    }


    public Channel? Get(string channelId) => this.repository.Get<Channel>(channelId);
    public IList<Channel> GetAll() => this.repository.GetList<Channel>().ToList();
}

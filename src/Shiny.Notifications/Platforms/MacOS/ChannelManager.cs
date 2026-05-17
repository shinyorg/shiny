using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Shiny.Support.Repositories;
using Foundation;
using UserNotifications;

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
                this.logger.LogDebug("Channel manager initialized successfully");
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
        var apple = channel.TryToNative<AppleChannel>();
        this.repository.Set(apple);
        this.RebuildNativeCategories();
    }


    public void Clear()
    {
        this.EnsureReady();
        this.repository.Clear<AppleChannel>();
        this.Add(Channel.Default);
    }


    public Channel? Get(string channelId)
    {
        this.EnsureReady();
        return this.repository.Get<AppleChannel>(channelId);
    }


    public IList<Channel> GetAll()
    {
        this.EnsureReady();
        return this.repository.GetAll<AppleChannel>().OfType<Channel>().ToList();
    }


    public void Remove(string channelId)
    {
        this.EnsureReady();
        this.AssertChannelRemove(channelId);

        this.repository.Remove<AppleChannel>(channelId);
        this.RebuildNativeCategories();
    }


    protected void RebuildNativeCategories()
    {
        var list = this.repository.GetAll<AppleChannel>();
        var categories = new List<UNNotificationCategory>();

        foreach (var channel in list)
        {
            var actions = new List<UNNotificationAction>();
            foreach (var action in channel.Actions)
            {
                var nativeAction = this.CreateAction(action);
                actions.Add(nativeAction);
            }

            var native = UNNotificationCategory.FromIdentifier(
                channel.Identifier,
                actions.ToArray(),
                channel.IntentIdentifiers ?? new[] { String.Empty },
                channel.CategoryOptions
            );
            categories.Add(native);
        }
        var set = new NSSet<UNNotificationCategory>(categories.ToArray());
        UNUserNotificationCenter.Current.SetNotificationCategories(set);
    }


    protected virtual UNNotificationAction CreateAction(ChannelAction action) => action.ActionType switch
    {
        ChannelActionType.TextReply => UNTextInputNotificationAction.FromIdentifier(
            action.Identifier,
            action.Title,
            UNNotificationActionOptions.None,
            action.Title,
            String.Empty
        ),

        ChannelActionType.Destructive => UNNotificationAction.FromIdentifier(
            action.Identifier,
            action.Title,
            UNNotificationActionOptions.Destructive
        ),

        ChannelActionType.OpenApp => UNNotificationAction.FromIdentifier(
            action.Identifier,
            action.Title,
            UNNotificationActionOptions.Foreground
        ),

        ChannelActionType.None => UNNotificationAction.FromIdentifier(
            action.Identifier,
            action.Title,
            UNNotificationActionOptions.None
        ),

        _ => throw new InvalidOperationException("Invalid action type")
    };
}

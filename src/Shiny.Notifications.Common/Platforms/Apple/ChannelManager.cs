using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Foundation;
using UserNotifications;
using Microsoft.Extensions.Logging;
using Shiny.Stores;

namespace Shiny.Notifications;


public class ChannelManager : IChannelManager, IShinyStartupTask
{
    readonly IRepository<Channel> repository;
    readonly ILogger<ChannelManager> logger;


    public ChannelManager(IRepository<Channel> repository, ILogger<ChannelManager> logger)
    {
        this.repository = repository;
        this.logger = logger;
    }


    public void Start()
    {
        this.logger.LogInformation("Starting iOS channel manager");
        this.Add(Channel.Default).ContinueWith(x =>
        {
            if (x.IsFaulted)
            {
                this.logger.LogError("Failed to create default channel", x.Exception);
            }
            else
            {
                this.logger.LogDebug("Channel manager initialized successfully");
            }
        });
    }


    public async Task Add(Channel channel)
    {
        channel.AssertValid();
        await this.repository.Set(channel).ConfigureAwait(false);
        await this.RebuildNativeCategories().ConfigureAwait(false);
    }


    public async Task Clear()
    {
        await this.repository.Clear().ConfigureAwait(false);

        // there must always be a default
        await this.Add(Channel.Default).ConfigureAwait(false);
    }


    public Task<Channel?> Get(string channelId) => this.repository.Get(channelId);
    public Task<IList<Channel>> GetAll() => this.repository.GetList();


    public async Task Remove(string channelId)
    {
        this.AssertChannelRemove(channelId);

        await this.repository.Remove(channelId).ConfigureAwait(false);
        await this.RebuildNativeCategories().ConfigureAwait(false);
    }


    protected async Task RebuildNativeCategories()
    {
        var channels = await this.GetAll().ConfigureAwait(false);
        var list = channels.ToList();

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
                new string[] { "" },
                UNNotificationCategoryOptions.None
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

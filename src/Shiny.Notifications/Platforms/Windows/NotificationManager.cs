using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Stores;
using Shiny.Support.Repositories;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Shiny.Notifications;


public class NotificationManager : INotificationManager
{
    readonly IServiceProvider services;
    readonly IChannelManager channelManager;
    readonly IRepository repository;
    readonly IKeyValueStore settings;
    readonly ILogger logger;

    string? aumid;
    ToastNotifier? notifier;
    ToastNotifier Notifier
    {
        get
        {
            if (this.notifier == null)
            {
                try
                {
                    _ = Windows.Storage.ApplicationData.Current.LocalSettings;
                    this.notifier = ToastNotificationManager.CreateToastNotifier();
                }
                catch (InvalidOperationException)
                {
                    // Unpackaged app – no package identity, so CreateToastNotifier()
                    // would throw. Fall back to the AUMID-based overload.
                    this.aumid = AppDomain.CurrentDomain.FriendlyName;
                    this.notifier = ToastNotificationManager.CreateToastNotifier(this.aumid);
                }
            }
            return this.notifier;
        }
    }


    public NotificationManager(
        IServiceProvider services,
        IChannelManager channelManager,
        IRepository repository,
        IKeyValueStoreFactory keystore,
        ILogger<NotificationManager> logger
    )
    {
        this.services = services;
        this.channelManager = channelManager;
        this.repository = repository;
        this.settings = keystore.DefaultStore;
        this.logger = logger;
    }


    public void AddChannel(Channel channel) => this.channelManager.Add(channel);
    public void RemoveChannel(string channelId) => this.channelManager.Remove(channelId);
    public void ClearChannels() => this.channelManager.Clear();
    public Channel? GetChannel(string channelId) => this.channelManager.Get(channelId);
    public IList<Channel> GetChannels() => this.channelManager.GetAll();


    public Task<AccessState> GetCurrentAccess(AccessRequestFlags flags = AccessRequestFlags.Notification)
    {
        try
        {
            var setting = this.Notifier.Setting;
            return Task.FromResult(setting switch
            {
                NotificationSetting.Enabled => AccessState.Available,
                NotificationSetting.DisabledForApplication => AccessState.Denied,
                NotificationSetting.DisabledForUser => AccessState.Denied,
                NotificationSetting.DisabledByGroupPolicy => AccessState.Restricted,
                NotificationSetting.DisabledByManifest => AccessState.NotSupported,
                _ => AccessState.Unknown
            });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to get notifier setting");
            return Task.FromResult(AccessState.NotSupported);
        }
    }


    public Task<AccessState> RequestAccess(AccessRequestFlags flags = AccessRequestFlags.Notification)
        => this.GetCurrentAccess(flags);


    public Task<Notification>? GetNotification(int notificationId)
    {
        var n = this.repository.Get<Notification>(notificationId.ToString());
        return n == null ? null : Task.FromResult(n);
    }


    public Task<IList<Notification>> GetPendingNotifications()
    {
        IList<Notification> list = this.repository.GetList<Notification>();
        return Task.FromResult(list);
    }


    public Task Send(Notification notification)
    {
        notification.AssertValid();

        if (notification.Geofence != null)
            throw new NotSupportedException("Geofence notifications are not supported on Windows");

        if (notification.Id == 0)
            notification.Id = this.settings.IncrementValue("NotificationId");

        var channel = Channel.Default;
        if (!notification.Channel.IsEmpty())
        {
            channel = this.channelManager.Get(notification.Channel!)
                ?? throw new InvalidOperationException($"Channel '{notification.Channel}' does not exist");
        }

        var xml = this.BuildToastXml(notification, channel);
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var tag = notification.Id.ToString();

        if (notification.ScheduleDate != null)
        {
            var scheduled = new ScheduledToastNotification(doc, notification.ScheduleDate.Value)
            {
                Id = tag,
                Tag = tag,
                Group = "shiny"
            };
            this.repository.Set(notification);
            this.Notifier.AddToSchedule(scheduled);
        }
        else if (notification.RepeatInterval != null)
        {
            var first = notification.RepeatInterval.CalculateNextAlarm();
            var firstOffset = new DateTimeOffset(first, TimeSpan.Zero);

            if (notification.RepeatInterval.Interval != null)
            {
                // Windows requires the snooze interval to be 60s..60d
                var interval = notification.RepeatInterval.Interval.Value;
                if (interval < TimeSpan.FromMinutes(1))
                    interval = TimeSpan.FromMinutes(1);
                if (interval > TimeSpan.FromDays(60))
                    interval = TimeSpan.FromDays(60);

                var scheduled = new ScheduledToastNotification(doc, firstOffset, interval, 5)
                {
                    Id = tag,
                    Tag = tag,
                    Group = "shiny"
                };
                this.repository.Set(notification);
                this.Notifier.AddToSchedule(scheduled);
            }
            else
            {
                // TimeOfDay (optionally with DayOfWeek): schedule the next occurrence only.
                var scheduled = new ScheduledToastNotification(doc, firstOffset)
                {
                    Id = tag,
                    Tag = tag,
                    Group = "shiny"
                };
                this.repository.Set(notification);
                this.Notifier.AddToSchedule(scheduled);
            }
        }
        else
        {
            var toast = new ToastNotification(doc)
            {
                Tag = tag,
                Group = "shiny"
            };
            this.repository.Set(notification);
            this.Notifier.Show(toast);
        }

        return Task.CompletedTask;
    }


    public Task Cancel(int id)
    {
        var tag = id.ToString();

        try
        {
            var scheduled = this.Notifier
                .GetScheduledToastNotifications()
                .FirstOrDefault(x => x.Tag == tag);

            if (scheduled != null)
                this.Notifier.RemoveFromSchedule(scheduled);

            if (this.aumid != null)
                ToastNotificationManager.History.Remove(tag, "shiny", this.aumid);
            else
                ToastNotificationManager.History.Remove(tag, "shiny");
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to cancel notification {Id}", id);
        }

        this.repository.Remove<Notification>(tag);
        return Task.CompletedTask;
    }


    public Task Cancel(CancelScope scope = CancelScope.All)
    {
        try
        {
            if (scope == CancelScope.All || scope == CancelScope.Pending)
            {
                foreach (var st in this.Notifier.GetScheduledToastNotifications())
                    this.Notifier.RemoveFromSchedule(st);
            }

            if (scope == CancelScope.All || scope == CancelScope.DisplayedOnly)
            {
                if (this.aumid != null)
                    ToastNotificationManager.History.Clear(this.aumid);
                else
                    ToastNotificationManager.History.Clear();
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to cancel notifications (scope: {Scope})", scope);
        }

        if (scope == CancelScope.All || scope == CancelScope.Pending)
            this.repository.Clear<Notification>();

        return Task.CompletedTask;
    }


    protected virtual string BuildToastXml(Notification notification, Channel channel)
    {
        var sb = new StringBuilder();
        sb.Append("<toast");

        var scenario = channel.Importance switch
        {
            ChannelImportance.Critical => "urgent",
            ChannelImportance.High => "reminder",
            _ => null
        };
        if (scenario != null)
            sb.Append($" scenario=\"{scenario}\"");

        if (notification.Payload?.Any() ?? false)
        {
            var launchArgs = string.Join("&amp;", notification.Payload.Select(kv => $"{Escape(kv.Key)}={Escape(kv.Value)}"));
            sb.Append($" launch=\"{launchArgs}\"");
        }
        sb.Append('>');

        sb.Append("<visual><binding template=\"ToastGeneric\">");
        if (!notification.Title.IsEmpty())
            sb.Append($"<text>{Escape(notification.Title!)}</text>");
        if (!notification.Message.IsEmpty())
            sb.Append($"<text>{Escape(notification.Message!)}</text>");
        sb.Append("</binding></visual>");

        if (channel.Actions.Any())
        {
            sb.Append("<actions>");
            foreach (var action in channel.Actions)
            {
                switch (action.ActionType)
                {
                    case ChannelActionType.TextReply:
                        sb.Append($"<input id=\"{Escape(action.Identifier)}\" type=\"text\" placeHolderContent=\"{Escape(action.Title)}\" />");
                        sb.Append($"<action content=\"{Escape(action.Title)}\" arguments=\"action={Escape(action.Identifier)}\" hint-inputId=\"{Escape(action.Identifier)}\" />");
                        break;

                    default:
                        sb.Append($"<action content=\"{Escape(action.Title)}\" arguments=\"action={Escape(action.Identifier)}\" />");
                        break;
                }
            }
            sb.Append("</actions>");
        }

        switch (channel.Sound)
        {
            case ChannelSound.None:
                sb.Append("<audio silent=\"true\" />");
                break;

            case ChannelSound.High:
                sb.Append("<audio src=\"ms-winsoundevent:Notification.Looping.Alarm\" loop=\"true\" />");
                break;

            case ChannelSound.Custom when !channel.CustomSoundPath.IsEmpty():
                sb.Append($"<audio src=\"{Escape(channel.CustomSoundPath!)}\" />");
                break;
        }

        sb.Append("</toast>");
        return sb.ToString();
    }


    static string Escape(string value) => System.Security.SecurityElement.Escape(value) ?? value;
}

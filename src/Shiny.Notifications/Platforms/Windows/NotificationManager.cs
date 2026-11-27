using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores;
using Shiny.Extensions.Stores.Repositories;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Shiny.Notifications;


public class NotificationManager(
    IChannelManager channelManager,
    IRepository repository,
    [FromKeyedServices(StoreKeys.Default)] IKeyValueStore settings,
    ILogger<NotificationManager> logger,
    IPlatform platform
) : INotificationManager, IShinyStartupTask
{
    // Windows has no repeating toast, so a repeating notification is scheduled this many occurrences ahead
    // and topped up each time the app starts
    internal const int RepeatOccurrences = 30;
    const string ToastGroup = "shiny";

    // toasts can't load remote images without package identity, so the image is stored locally and referenced by file:///
    string ImageDirectory => System.IO.Path.Combine(platform.AppData.FullName, "shiny-notification-images");

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


    public void AddChannel(Channel channel) => channelManager.Add(channel);
    public void RemoveChannel(string channelId) => channelManager.Remove(channelId);
    public void ClearChannels() => channelManager.Clear();
    public Channel? GetChannel(string channelId) => channelManager.Get(channelId);
    public IList<Channel> GetChannels() => channelManager.GetAll();


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
            // Unpackaged apps have no package identity, so ToastNotifier.Setting throws
            // ELEMENT_NOT_FOUND (0x80070490). Toasts still work in that scenario once the
            // AUMID is registered, so assume access is available rather than reporting a
            // failure that would block the app from sending notifications.
            logger.LogDebug(ex, "Unable to read notifier setting - assuming notifications are available (likely an unpackaged app)");
            return Task.FromResult(AccessState.Available);
        }
    }


    public Task<AccessState> RequestAccess(AccessRequestFlags flags = AccessRequestFlags.Notification)
        => this.GetCurrentAccess(flags);


    public Task<Notification>? GetNotification(int notificationId)
        => Task.FromResult(repository.Get<Notification>(notificationId.ToString())!);


    public void Start()
    {
        try
        {
            var repeating = repository
                .GetAll<Notification>()
                .Where(x => x.RepeatInterval != null)
                .ToList();

            foreach (var notification in repeating)
            {
                var channel = notification.Channel.IsEmpty()
                    ? Channel.Default
                    : channelManager.Get(notification.Channel!) ?? Channel.Default;

                this.ScheduleRepeating(notification, this.BuildDocument(notification, channel));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to top up repeating notifications");
        }
    }


    public Task<IList<Notification>> GetPendingNotifications()
    {
        IList<Notification> list = repository.GetAll<Notification>().ToList();
        return Task.FromResult(list);
    }


    public async Task Send(Notification notification)
    {
        notification.AssertValid();

        if (notification.Geofence != null)
            throw new NotSupportedException("Geofence notifications are not supported on Windows");

        if (notification.Id == 0)
            notification.Id = settings.IncrementValue("NotificationId");

        var channel = Channel.Default;
        if (!notification.Channel.IsEmpty())
        {
            channel = channelManager.Get(notification.Channel!)
                ?? throw new InvalidOperationException($"Channel '{notification.Channel}' does not exist");
        }

        if (notification.ImageUri.IsEmpty())
            NotificationImages.Delete(this.ImageDirectory, notification.Id.ToString());
        else
            await NotificationImages.TryStore(notification.ImageUri!, this.ImageDirectory, notification.Id.ToString(), logger).ConfigureAwait(false);

        var doc = this.BuildDocument(notification, channel);
        var tag = notification.Id.ToString();

        // a notification sent again with the same id replaces what was scheduled for it
        this.RemoveScheduled(tag);

        // only what is still to come is pending - a toast shown now is not
        if (notification.ScheduleDate != null || notification.RepeatInterval != null)
            repository.Set(notification);
        else
            repository.Remove<Notification>(tag);

        if (notification.ScheduleDate != null)
        {
            this.Notifier.AddToSchedule(new ScheduledToastNotification(doc, notification.ScheduleDate.Value)
            {
                Id = tag,
                Tag = tag,
                Group = ToastGroup
            });
        }
        else if (notification.RepeatInterval != null)
        {
            this.ScheduleRepeating(notification, doc);
        }
        else
        {
            this.Notifier.Show(new ToastNotification(doc)
            {
                Tag = tag,
                Group = ToastGroup
            });
        }
    }


    XmlDocument BuildDocument(Notification notification, Channel channel)
    {
        var doc = new XmlDocument();
        doc.LoadXml(this.BuildToastXml(notification, channel));
        return doc;
    }


    void ScheduleRepeating(Notification notification, XmlDocument doc)
    {
        var tag = notification.Id.ToString();
        this.RemoveScheduled(tag);

        var trigger = notification.RepeatInterval!;
        var at = trigger.TimeProvider.GetUtcNow();

        // Windows rejects a scheduled toast less than a minute after the previous one
        var interval = trigger.Interval;
        if (interval != null && interval.Value < TimeSpan.FromMinutes(1))
            trigger = new IntervalTrigger { Interval = TimeSpan.FromMinutes(1), TimeProvider = trigger.TimeProvider };

        for (var i = 0; i < RepeatOccurrences; i++)
        {
            at = trigger.CalculateNextAlarm(at);
            this.Notifier.AddToSchedule(new ScheduledToastNotification(doc, at)
            {
                Id = $"{tag}_{i}",
                Tag = tag,
                Group = ToastGroup
            });
        }
    }


    void RemoveScheduled(string tag)
    {
        var scheduled = this.Notifier
            .GetScheduledToastNotifications()
            .Where(x => x.Tag == tag)
            .ToList();

        foreach (var toast in scheduled)
            this.Notifier.RemoveFromSchedule(toast);
    }


    public Task Cancel(int id)
    {
        var tag = id.ToString();

        try
        {
            this.RemoveScheduled(tag);

            if (this.aumid != null)
                ToastNotificationManager.History.Remove(tag, "shiny", this.aumid);
            else
                ToastNotificationManager.History.Remove(tag, "shiny");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to cancel notification {Id}", id);
        }

        repository.Remove<Notification>(tag);
        NotificationImages.Delete(this.ImageDirectory, tag);
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
            logger.LogWarning(ex, "Failed to cancel notifications (scope: {Scope})", scope);
        }

        if (scope == CancelScope.All || scope == CancelScope.Pending)
            repository.Clear<Notification>();

        if (scope == CancelScope.All)
        {
            try
            {
                if (Directory.Exists(this.ImageDirectory))
                    Directory.Delete(this.ImageDirectory, true);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Failed to remove notification images");
            }
        }

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
        var image = notification.ImageUri.IsEmpty() ? null : NotificationImages.Find(this.ImageDirectory, notification.Id.ToString());
        if (image != null)
            sb.Append($"<image placement=\"hero\" src=\"{Escape(new Uri(image).AbsoluteUri)}\" />");
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

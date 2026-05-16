using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Notifications.Freedesktop;
using Tmds.DBus.Protocol;

namespace Shiny.Notifications;


public class NotificationManager(
    IServiceProvider services,
    IChannelManager channelManager,
    ILogger<NotificationManager> logger
) : INotificationManager, IAsyncDisposable
{
    // In-memory persistence. Linux desktop notifications via freedesktop don't have a concept
    // of persistent scheduled notifications anyway — they live for the duration of the process.
    readonly ConcurrentDictionary<int, Notification> pending = new();

    // Maps the freedesktop daemon-assigned id (uint) to our Shiny notification id (int)
    readonly ConcurrentDictionary<uint, int> nativeToShinyId = new();

    int nextId;
    Connection? connection;
    Timer? scheduler;

    // public void ComponentStart()
    // {
    //     _ = this.EnsureConnectionAsync();
    //     this.scheduler = new Timer(_ => _ = this.RunScheduledAsync(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
    // }


    public void AddChannel(Channel channel) => channelManager.Add(channel);
    public void RemoveChannel(string channelId) => channelManager.Remove(channelId);
    public void ClearChannels() => channelManager.Clear();
    public Channel? GetChannel(string channelId) => channelManager.Get(channelId);
    public IList<Channel> GetChannels() => channelManager.GetAll();


    public async Task<AccessState> GetCurrentAccess(AccessRequestFlags flags = AccessRequestFlags.Notification)
    {
        try
        {
            await this.EnsureConnectionAsync().ConfigureAwait(false);
            var writer = this.connection!.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: FreedesktopConstants.Service,
                path: FreedesktopConstants.ObjectPath,
                @interface: FreedesktopConstants.Interface,
                member: "GetCapabilities"
            );
            var msg = writer.CreateMessage();
            await this.connection.CallMethodAsync(msg).ConfigureAwait(false);
            return AccessState.Available;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "freedesktop notifications daemon not reachable");
            return AccessState.NotSupported;
        }
    }


    public Task<AccessState> RequestAccess(AccessRequestFlags flags = AccessRequestFlags.Notification)
        => this.GetCurrentAccess(flags);


    public Task<Notification>? GetNotification(int notificationId)
        => this.pending.TryGetValue(notificationId, out var n) ? Task.FromResult(n) : null;


    public Task<IList<Notification>> GetPendingNotifications()
    {
        IList<Notification> list = this.pending.Values.ToList();
        return Task.FromResult(list);
    }


    public async Task Send(Notification notification)
    {
        notification.AssertValid();

        if (notification.Geofence != null)
            throw new NotSupportedException("Geofence notifications are not supported on Linux");

        if (notification.Id == 0)
            notification.Id = Interlocked.Increment(ref this.nextId);

        var channel = Channel.Default;
        if (!notification.Channel.IsEmpty())
        {
            channel = channelManager.Get(notification.Channel!)
                ?? throw new InvalidOperationException($"Channel '{notification.Channel}' does not exist");
        }

        if (notification.ScheduleDate == null && notification.RepeatInterval == null)
        {
            await this.SendNativeAsync(notification, channel).ConfigureAwait(false);
        }
        else
        {
            this.pending[notification.Id] = notification;
        }
    }


    public Task Cancel(int id)
    {
        var nativeId = this.nativeToShinyId.FirstOrDefault(kv => kv.Value == id).Key;
        if (nativeId != 0)
            _ = this.CloseNativeAsync(nativeId);

        this.pending.TryRemove(id, out _);
        return Task.CompletedTask;
    }


    public Task Cancel(CancelScope scope = CancelScope.All)
    {
        if (scope == CancelScope.All || scope == CancelScope.DisplayedOnly)
        {
            foreach (var nativeId in this.nativeToShinyId.Keys.ToList())
                _ = this.CloseNativeAsync(nativeId);
        }

        if (scope == CancelScope.All || scope == CancelScope.Pending)
            this.pending.Clear();

        return Task.CompletedTask;
    }


    public ValueTask DisposeAsync()
    {
        this.scheduler?.Dispose();
        this.connection?.Dispose();
        this.connection = null;
        return ValueTask.CompletedTask;
    }


    async Task SendNativeAsync(Notification notification, Channel channel)
    {
        try
        {
            await this.EnsureConnectionAsync().ConfigureAwait(false);

            var writer = this.connection!.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: FreedesktopConstants.Service,
                path: FreedesktopConstants.ObjectPath,
                @interface: FreedesktopConstants.Interface,
                member: "Notify",
                signature: "susssasa{sv}i"
            );

            writer.WriteString("Shiny");                              // app_name
            writer.WriteUInt32(0);                                    // replaces_id (0 = new)
            writer.WriteString("");                                   // app_icon
            writer.WriteString(notification.Title ?? string.Empty);   // summary
            writer.WriteString(notification.Message ?? string.Empty); // body

            // actions: alternating identifier/label pairs (as)
            var actions = new List<string>();
            foreach (var a in channel.Actions)
            {
                actions.Add(a.Identifier);
                actions.Add(a.Title);
            }
            writer.WriteArray(actions.ToArray());

            // hints: a{sv}
            var hints = new Dictionary<string, VariantValue>();
            var urgency = channel.Importance switch
            {
                ChannelImportance.Critical => (byte)2,
                ChannelImportance.High => (byte)2,
                ChannelImportance.Normal => (byte)1,
                _ => (byte)0
            };
            hints["urgency"] = VariantValue.Byte(urgency);
            if (channel.Sound == ChannelSound.None)
                hints["suppress-sound"] = VariantValue.Bool(true);
            writer.WriteDictionary(hints);

            writer.WriteInt32(-1); // expire_timeout (-1 = default)

            var msg = writer.CreateMessage();
            var nativeId = await this.connection.CallMethodAsync(
                msg,
                static (Message reply, object? _) =>
                {
                    var reader = reply.GetBodyReader();
                    return reader.ReadUInt32();
                }
            ).ConfigureAwait(false);

            this.nativeToShinyId[nativeId] = notification.Id;
            logger.LogDebug("Sent notification {ShinyId} as freedesktop id {NativeId}", notification.Id, nativeId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send notification {Id}", notification.Id);
        }
    }


    async Task CloseNativeAsync(uint nativeId)
    {
        try
        {
            await this.EnsureConnectionAsync().ConfigureAwait(false);
            var writer = this.connection!.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: FreedesktopConstants.Service,
                path: FreedesktopConstants.ObjectPath,
                @interface: FreedesktopConstants.Interface,
                member: "CloseNotification",
                signature: "u"
            );
            writer.WriteUInt32(nativeId);
            var msg = writer.CreateMessage();
            await this.connection.CallMethodAsync(msg).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to close native notification {NativeId}", nativeId);
        }
    }


    async Task EnsureConnectionAsync()
    {
        if (this.connection != null) return;
        this.connection = new Connection(Address.Session!);
        await this.connection.ConnectAsync().ConfigureAwait(false);
    }


    async Task RunScheduledAsync()
    {
        try
        {
            var now = DateTime.Now;
            foreach (var n in this.pending.Values.ToList())
            {
                var channel = Channel.Default;
                if (!n.Channel.IsEmpty())
                {
                    var ch = channelManager.Get(n.Channel!);
                    if (ch != null) channel = ch;
                }

                if (n.ScheduleDate != null && n.ScheduleDate.Value <= now)
                {
                    await this.SendNativeAsync(n, channel).ConfigureAwait(false);
                    this.pending.TryRemove(n.Id, out _);
                }
                else if (n.RepeatInterval != null)
                {
                    var next = n.RepeatInterval.CalculateNextAlarm();
                    if (next <= now)
                        await this.SendNativeAsync(n, channel).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scheduled notifications run failed");
        }
    }
}

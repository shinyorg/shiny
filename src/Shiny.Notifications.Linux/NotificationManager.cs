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
) : INotificationManager, IShinyStartupTask, IAsyncDisposable
{
    // In-memory persistence. Linux desktop notifications via freedesktop don't have a concept
    // of persistent scheduled notifications anyway — they live for the duration of the process.
    readonly ConcurrentDictionary<int, Notification> pending = new();

    // Maps the freedesktop daemon-assigned id (uint) to the Shiny notification on screen, until the daemon reports it closed
    readonly ConcurrentDictionary<uint, Notification> displayed = new();

    // Next fire time for each repeating notification. Held here rather than recalculated on every
    // scheduler tick - a fresh calculation always lands in the future, so it would never come due.
    readonly ConcurrentDictionary<int, DateTimeOffset> nextFire = new();

    readonly SemaphoreSlim connectGate = new(1, 1);
    int nextId;
    DBusConnection? connection;
    Timer? scheduler;
    bool disposed;

    public void Start()
    {
        _ = this.EnsureConnectionAsync();
        this.scheduler = new Timer(_ => _ = this.RunScheduledAsync(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
    }


    public void AddChannel(Channel channel) => channelManager.Add(channel);
    public void RemoveChannel(string channelId) => channelManager.Remove(channelId);
    public void ClearChannels() => channelManager.Clear();
    public Channel? GetChannel(string channelId) => channelManager.Get(channelId);
    public IList<Channel> GetChannels() => channelManager.GetAll();


    public async Task<AccessState> GetCurrentAccess(AccessRequestFlags flags = AccessRequestFlags.Notification)
    {
        try
        {
            var conn = await this.EnsureConnectionAsync().ConfigureAwait(false);
            var writer = conn.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: FreedesktopConstants.Service,
                path: FreedesktopConstants.ObjectPath,
                @interface: FreedesktopConstants.Interface,
                member: "GetCapabilities"
            );
            var msg = writer.CreateMessage();
            await conn.CallMethodAsync(msg).ConfigureAwait(false);
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
        => Task.FromResult(this.pending.TryGetValue(notificationId, out var n) ? n : null!);


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
            if (notification.RepeatInterval == null)
                this.nextFire.TryRemove(notification.Id, out _);
            else
                this.nextFire[notification.Id] = CalculateNextFire(notification.RepeatInterval);

            this.pending[notification.Id] = notification;
        }
    }


    public Task Cancel(int id)
    {
        var nativeIds = this.displayed
            .Where(kv => kv.Value.Id == id)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var nativeId in nativeIds)
            _ = this.CloseNativeAsync(nativeId);

        this.pending.TryRemove(id, out _);
        this.nextFire.TryRemove(id, out _);
        return Task.CompletedTask;
    }


    public Task Cancel(CancelScope scope = CancelScope.All)
    {
        if (scope == CancelScope.All || scope == CancelScope.DisplayedOnly)
        {
            foreach (var nativeId in this.displayed.Keys.ToList())
                _ = this.CloseNativeAsync(nativeId);
        }

        if (scope == CancelScope.All || scope == CancelScope.Pending)
        {
            this.pending.Clear();
            this.nextFire.Clear();
        }

        return Task.CompletedTask;
    }


    public ValueTask DisposeAsync()
    {
        this.disposed = true;
        this.scheduler?.Dispose();
        this.connection?.Dispose();
        this.connection = null;
        return ValueTask.CompletedTask;
    }


    async Task SendNativeAsync(Notification notification, Channel channel)
    {
        try
        {
            var conn = await this.EnsureConnectionAsync().ConfigureAwait(false);

            var writer = conn.GetMessageWriter();
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

            // actions: alternating identifier/label pairs (as). "default" is the action the daemon invokes when the
            // notification itself is clicked - servers don't show it as a button
            var actions = new List<string> { DefaultActionKey, "Open" };
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
            var nativeId = await conn.CallMethodAsync(
                msg,
                static (Message reply, object? _) =>
                {
                    var reader = reply.GetBodyReader();
                    return reader.ReadUInt32();
                }
            ).ConfigureAwait(false);

            this.displayed[nativeId] = notification;
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
            var conn = await this.EnsureConnectionAsync().ConfigureAwait(false);
            var writer = conn.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: FreedesktopConstants.Service,
                path: FreedesktopConstants.ObjectPath,
                @interface: FreedesktopConstants.Interface,
                member: "CloseNotification",
                signature: "u"
            );
            writer.WriteUInt32(nativeId);
            var msg = writer.CreateMessage();
            await conn.CallMethodAsync(msg).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to close native notification {NativeId}", nativeId);
        }
    }


    const string DefaultActionKey = "default";


    /// <summary>
    /// One connection, shared by every call. A failed connect is not kept, and a connection the bus drops is forgotten,
    /// so the next call connects again instead of notifications staying broken until the process restarts.
    /// </summary>
    async Task<DBusConnection> EnsureConnectionAsync()
    {
        var current = this.connection;
        if (current != null)
            return current;

        await this.connectGate.WaitAsync().ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);
            if (this.connection != null)
                return this.connection;

            var address = DBusAddress.Session
                ?? throw new InvalidOperationException("No D-Bus session bus address is set - DBUS_SESSION_BUS_ADDRESS is empty");

            var created = new DBusConnection(address);
            try
            {
                await created.ConnectAsync().ConfigureAwait(false);
            }
            catch
            {
                created.Dispose();
                throw;
            }

            await this.TryWatchSignalsAsync(created).ConfigureAwait(false);
            this.connection = created;
            _ = this.ForgetWhenDisconnected(created);
            return created;
        }
        finally
        {
            this.connectGate.Release();
        }
    }


    async Task ForgetWhenDisconnected(DBusConnection conn)
    {
        try
        {
            var reason = await conn.DisconnectedAsync().ConfigureAwait(false);
            if (reason != null && !this.disposed)
                logger.LogWarning(reason, "freedesktop notifications connection was lost - reconnecting on the next call");
        }
        catch
        {
            // the reason does not matter here - only that it is gone
        }
        Interlocked.CompareExchange(ref this.connection, null, conn);
    }


    /// <summary>
    /// Subscribes to the daemon's ActionInvoked (a click or action button) and NotificationClosed signals. Notifications
    /// still send without them, so a failure here is logged rather than thrown.
    /// </summary>
    async Task TryWatchSignalsAsync(DBusConnection conn)
    {
        try
        {
            await conn.AddMatchAsync(
                new MatchRule
                {
                    Type = MessageType.Signal,
                    Path = FreedesktopConstants.ObjectPath,
                    Interface = FreedesktopConstants.Interface,
                    Member = "ActionInvoked"
                },
                static (Message msg, object? _) =>
                {
                    // ActionInvoked (us): read here, while the message is current, into plain values. Any app on the
                    // session bus can emit a signal under this name, so a malformed one reads as id 0 (never ours)
                    // rather than throwing inside Tmds
                    try
                    {
                        var reader = msg.GetBodyReader();
                        var id = reader.ReadUInt32();
                        var actionKey = reader.ReadString();
                        return (Id: id, ActionKey: actionKey);
                    }
                    catch
                    {
                        return (Id: 0u, ActionKey: string.Empty);
                    }
                },
                static (Tmds.DBus.Protocol.Notification<(uint Id, string ActionKey)> n) =>
                {
                    // IsCompletion first: on a value notification reading Exception throws, and a handler that
                    // throws disconnects the whole connection (CLAUDE.md, D-Bus rule 3)
                    if (n.IsCompletion)
                        return;

                    var manager = (NotificationManager)n.State!;
                    try
                    {
                        manager.OnActionInvoked(n.Value.Id, n.Value.ActionKey);
                    }
                    catch (Exception ex)
                    {
                        manager.LogSignalError(ex, "ActionInvoked");
                    }
                },
                emitOnCapturedContext: false,
                ObserverFlags.None,
                this
            ).ConfigureAwait(false);

            await conn.AddMatchAsync(
                new MatchRule
                {
                    Type = MessageType.Signal,
                    Path = FreedesktopConstants.ObjectPath,
                    Interface = FreedesktopConstants.Interface,
                    Member = "NotificationClosed"
                },
                static (Message msg, object? _) =>
                {
                    // NotificationClosed (uu): only the id is needed
                    try
                    {
                        var reader = msg.GetBodyReader();
                        return reader.ReadUInt32();
                    }
                    catch
                    {
                        return 0u;
                    }
                },
                static (Tmds.DBus.Protocol.Notification<uint> n) =>
                {
                    if (n.IsCompletion)
                        return;

                    var manager = (NotificationManager)n.State!;
                    try
                    {
                        manager.displayed.TryRemove(n.Value, out _);
                    }
                    catch (Exception ex)
                    {
                        manager.LogSignalError(ex, "NotificationClosed");
                    }
                },
                emitOnCapturedContext: false,
                ObserverFlags.None,
                this
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not watch freedesktop notification signals - taps and actions will not reach INotificationDelegate");
        }
    }


    void LogSignalError(Exception ex, string signal)
        => logger.LogError(ex, "Error processing {Signal}", signal);


    void OnActionInvoked(uint nativeId, string actionKey)
    {
        // the signal is broadcast for every app's notifications - only ours are in the map
        if (!this.displayed.TryGetValue(nativeId, out var notification))
            return;

        var action = actionKey == DefaultActionKey ? null : actionKey;
        var response = new NotificationResponse(notification, action, null);
        _ = services.RunDelegates<INotificationDelegate>(x => x.OnEntry(response), logger);
    }


    async Task RunScheduledAsync()
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
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
                else if (
                    n.RepeatInterval != null &&
                    this.nextFire.TryGetValue(n.Id, out var due) &&
                    due <= n.RepeatInterval.TimeProvider.GetUtcNow()
                )
                {
                    // advance before sending so a slow D-Bus call can't fire the same occurrence twice
                    this.nextFire[n.Id] = CalculateNextFire(n.RepeatInterval);
                    await this.SendNativeAsync(n, channel).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scheduled notifications run failed");
        }
    }


    /// <summary>
    /// The next time a repeating trigger is due, strictly after the trigger's current clock time.
    /// Interval triggers fire every Interval from now; TimeOfDay triggers fire at that local time -
    /// today if it is still ahead, otherwise the next day (matching DayOfWeek when one is set).
    /// </summary>
    internal static DateTimeOffset CalculateNextFire(IntervalTrigger trigger)
    {
        var now = trigger.TimeProvider.GetUtcNow();
        if (trigger.Interval != null)
            return now.Add(trigger.Interval.Value);

        var tz = trigger.TimeProvider.LocalTimeZone;
        var time = trigger.TimeOfDay!.Value;
        var day = TimeZoneInfo.ConvertTime(now, tz).Date;
        var next = AtLocal(day, time, tz);

        while (next <= now || (trigger.DayOfWeek != null && day.DayOfWeek != trigger.DayOfWeek.Value))
        {
            day = day.AddDays(1);
            next = AtLocal(day, time, tz);
        }
        return next;
    }


    static DateTimeOffset AtLocal(DateTime day, TimeSpan time, TimeZoneInfo tz)
    {
        var local = DateTime.SpecifyKind(day.Add(time), DateTimeKind.Unspecified);
        return new DateTimeOffset(local, tz.GetUtcOffset(local));
    }
}

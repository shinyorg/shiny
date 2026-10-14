using Shiny.Locations;
using Shiny.Notifications;

namespace Shiny.DeviceTests.Tests;


public class NotificationTests(INotificationManager notifications) : DeviceTestSuite
{
    const string TestChannelId = "devicetests";

    public override string Name => "Notifications";


    // the scheduled/delivered tests need permission - request it once up front (the runner's alert watcher
    // accepts the iOS prompt, Android's POST_NOTIFICATIONS is pre-granted)
    public override async Task SetupAsync(TestContext context)
    {
        var access = await notifications.RequestAccess().ConfigureAwait(false);
        context.Log($"Notification access: {access}");
        if (access != AccessState.Available)
            throw new SkipException($"Notification permission is {access}");

        await notifications.Cancel(CancelScope.All).ConfigureAwait(false);
    }


    public override async Task TeardownAsync(TestContext context)
    {
        await notifications.Cancel(CancelScope.All).ConfigureAwait(false);
        if (notifications.GetChannel(TestChannelId) != null)
            notifications.RemoveChannel(TestChannelId);
    }


    protected override void Define()
    {
        this.Test("Access_IsAvailable", async _ =>
        {
            var access = await notifications.GetCurrentAccess().ConfigureAwait(false);
            Assert.Equal(AccessState.Available, access);
        });

        this.Test("Channels_DefaultAlwaysExists", _ =>
        {
            var channel = notifications.GetChannel(Channel.Default.Identifier);
            Assert.NotNull(channel);
            Assert.Contains(notifications.GetChannels(), x => x.Identifier == Channel.Default.Identifier);
            return Task.CompletedTask;
        });

        this.Test("Channels_AddGetRemove", ctx =>
        {
            notifications.AddChannel(new Channel
            {
                Identifier = TestChannelId,
                Description = "Device tests",
                Importance = ChannelImportance.High,
                Actions = [ChannelAction.Create("reply", "Reply", ChannelActionType.TextReply)]
            });

            var channel = notifications.GetChannel(TestChannelId);
            Assert.NotNull(channel);
            Assert.Equal(ChannelImportance.High, channel.Importance);
            Assert.Equal("Device tests", channel.Description);
            var action = Assert.Single(channel.Actions);
            Assert.Equal("reply", action.Identifier);
            Assert.Equal(ChannelActionType.TextReply, action.ActionType);
            Assert.Contains(notifications.GetChannels(), x => x.Identifier == TestChannelId);

            notifications.RemoveChannel(TestChannelId);
            Assert.Null(notifications.GetChannel(TestChannelId));
            Assert.DoesNotContain(notifications.GetChannels(), x => x.Identifier == TestChannelId);
            return Task.CompletedTask;
        });

        this.Test("Send_UnknownChannel_Throws", async ctx =>
        {
            Exception? ex = null;
            try
            {
                await notifications.Send(new Notification
                {
                    Id = NewId(),
                    Title = "Device test",
                    Message = "Unknown channel",
                    Channel = "does-not-exist"
                }).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ex = e;
            }
            Assert.NotNull(ex);
            ctx.Log($"{ex.GetType().Name}: {ex.Message}");
        });

        this.Test("Send_Immediate_IsDelivered", async ctx =>
        {
            var id = NewId();
            await notifications.Send(new Notification
            {
                Id = id,
                Title = "Device test",
                Message = "Immediate"
            }).ConfigureAwait(false);

            await DeliveredNotifications.WaitForDeliveryAsync(ctx, id, TimeSpan.FromSeconds(15)).ConfigureAwait(false);

            await notifications.Cancel(id).ConfigureAwait(false);
            await DeliveredNotifications.WaitForRemovalAsync(ctx, id, TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        });

        this.Test("Schedule_IsPending_ThenCancel", async ctx =>
        {
            var id = NewId();
            var when = DateTimeOffset.Now.AddHours(1);
            await notifications.Send(new Notification
            {
                Id = id,
                Title = "Device test",
                Message = "Scheduled",
                ScheduleDate = when,
                Payload = new Dictionary<string, string> { ["key"] = "value" }
            }).ConfigureAwait(false);

            var pending = await notifications.GetPendingNotifications().ConfigureAwait(false);
            var match = Assert.Single(pending, x => x.Id == id);
            Assert.Equal("Scheduled", match.Message);
            Assert.Equal("Device test", match.Title);
            Assert.Equal("value", match.Payload["key"]);
            Assert.NotNull(match.ScheduleDate);
            Assert.InRange((match.ScheduleDate!.Value - when).Duration().TotalSeconds, 0, 60);

            var single = await notifications.GetNotification(id)!.ConfigureAwait(false);
            Assert.NotNull(single);

            await notifications.Cancel(id).ConfigureAwait(false);
            pending = await notifications.GetPendingNotifications().ConfigureAwait(false);
            Assert.DoesNotContain(pending, x => x.Id == id);
        });

        this.Test("Schedule_DeliversWhileForeground", async ctx =>
        {
            var id = NewId();
            await notifications.Send(new Notification
            {
                Id = id,
                Title = "Device test",
                Message = "Scheduled 5s out",
                ScheduleDate = DateTimeOffset.Now.AddSeconds(5)
            }).ConfigureAwait(false);

            Assert.Contains(await notifications.GetPendingNotifications().ConfigureAwait(false), x => x.Id == id);

            // Android alarms can be batched - give it well over the 5s
            await DeliveredNotifications.WaitForDeliveryAsync(ctx, id, TimeSpan.FromSeconds(75)).ConfigureAwait(false);

            var pending = await notifications.GetPendingNotifications().ConfigureAwait(false);
            Assert.DoesNotContain(pending, x => x.Id == id);
        }, TimeSpan.FromSeconds(100));

        this.Test("RepeatInterval_IsPending", async ctx =>
        {
            var id = NewId();
            await notifications.Send(new Notification
            {
                Id = id,
                Title = "Device test",
                Message = "Daily at 8",
                RepeatInterval = new IntervalTrigger { TimeOfDay = TimeSpan.FromHours(8) }
            }).ConfigureAwait(false);

            var pending = await notifications.GetPendingNotifications().ConfigureAwait(false);
            var match = Assert.Single(pending, x => x.Id == id);
            ctx.Log($"Pending repeat: ScheduleDate={match.ScheduleDate} TimeOfDay={match.RepeatInterval?.TimeOfDay}");
            Assert.NotNull(match.RepeatInterval);
            Assert.Equal(TimeSpan.FromHours(8), match.RepeatInterval.TimeOfDay);
        });

        this.Test("Geofence_IsPending", async ctx =>
        {
            var access = await notifications.GetCurrentAccess(AccessRequestFlags.LocationAware).ConfigureAwait(false);
            ctx.SkipUnless(access == AccessState.Available, $"Location-aware notification access is {access}");

            var id = NewId();
            await notifications.Send(new Notification
            {
                Id = id,
                Title = "Device test",
                Message = "Geofence",
                Geofence = new GeofenceTrigger
                {
                    Center = new Position(43.6532, -79.3832),
                    Radius = Distance.FromMeters(200)
                }
            }).ConfigureAwait(false);

            var pending = await notifications.GetPendingNotifications().ConfigureAwait(false);
            var match = Assert.Single(pending, x => x.Id == id);
            Assert.NotNull(match.Geofence);
            Assert.NotNull(match.Geofence.Center);
            Assert.Equal(43.6532, match.Geofence.Center.Latitude, 3);
            Assert.Equal(-79.3832, match.Geofence.Center.Longitude, 3);

            await notifications.Cancel(id).ConfigureAwait(false);
            Assert.DoesNotContain(await notifications.GetPendingNotifications().ConfigureAwait(false), x => x.Id == id);
        });

        this.Test("CancelPending_ClearsAll", async _ =>
        {
            foreach (var minutes in new[] { 30, 60 })
            {
                await notifications.Send(new Notification
                {
                    Id = NewId(),
                    Title = "Device test",
                    Message = $"{minutes}m",
                    ScheduleDate = DateTimeOffset.Now.AddMinutes(minutes)
                }).ConfigureAwait(false);
            }
            Assert.True((await notifications.GetPendingNotifications().ConfigureAwait(false)).Count >= 2);

            await notifications.Cancel(CancelScope.Pending).ConfigureAwait(false);
            Assert.Empty(await notifications.GetPendingNotifications().ConfigureAwait(false));
        });

        this.Test("Send_WithoutMessage_Throws", async _ =>
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => notifications.Send(new Notification { Title = "No message" }));
        });

        this.Test("Badge_SetGet", async ctx =>
        {
            var set = await notifications.TrySetBadge(3).ConfigureAwait(false);
            ctx.SkipUnless(set, "This platform's notification manager has no badge support");
            try
            {
                var (success, value) = await notifications.TryGetBadge().ConfigureAwait(false);
                Assert.True(success);
                Assert.Equal(3, value);
            }
            finally
            {
                await notifications.TrySetBadge(0).ConfigureAwait(false);
            }
        });
    }


    // well above the ids Shiny auto-assigns, so nothing the app sent itself is touched
    static int NewId() => Random.Shared.Next(700_000, 800_000);
}

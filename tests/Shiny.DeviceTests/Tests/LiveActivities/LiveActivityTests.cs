using Shiny.LiveActivities;

namespace Shiny.DeviceTests.Tests;


public class LiveActivityTests(ILiveActivityManager activities) : DeviceTestSuite
{
    // Android posts every live activity on this channel (Platforms/Android/LiveActivityManager.ChannelId)
    const string AndroidChannelId = "shiny_live_activities";

    public override string Name => "LiveActivities";


    public override async Task TeardownAsync(TestContext context)
        => await activities.EndAll().ConfigureAwait(false);


    protected override void Define()
    {
        this.Test("IsSupported_MatchesPlatform", ctx =>
        {
            // ActivityKit is unavailable on Mac Catalyst - it gets NoOpLiveActivityManager
            Assert.Equal(ctx.Platform != "maccatalyst", activities.IsSupported);
            return Task.CompletedTask;
        });

        this.Test("Unsupported_IsNoOp", async ctx =>
        {
            ctx.SkipUnless(!activities.IsSupported, "Live Activities are supported here");

            Assert.Equal(AccessState.NotSupported, await activities.GetCurrentAccess().ConfigureAwait(false));
            Assert.Empty(activities.GetAll());
            Assert.Null(activities.PushToStartToken);
            await activities.EndAll().ConfigureAwait(false);
        });

        this.Test("StartUpdateEnd", async ctx =>
        {
            ctx.SkipUnless(activities.IsSupported, "Live Activities are not supported here");

            var access = await activities.RequestAccess(ctx.CancellationToken).ConfigureAwait(false);
            ctx.Log($"Access: {access}");
            ctx.SkipUnless(access == AccessState.Available, $"Live Activity access is {access}");

            LiveActivity activity;
            try
            {
                activity = await activities.Start(new LiveActivityRequest
                {
                    Kind = "devicetests",
                    Attributes = new Dictionary<string, string> { ["run"] = "device-tests" },
                    // a push token needs the aps-environment entitlement this app does not have
                    RequestPushToken = false,
                    Content = new LiveActivityContent
                    {
                        Title = "Device test",
                        Body = "Started",
                        ShortStatus = "0%",
                        Progress = LiveActivityProgress.FromValue(0)
                    }
                }, ctx.CancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex) when (ctx.IsApple)
            {
                // ActivityKit can refuse an app that ships no widget extension
                throw new SkipException($"ActivityKit refused to start without a widget extension: {ex.Message}");
            }
            ctx.Log($"Started {activity.Id}");

            Assert.Equal(LiveActivityState.Active, activity.State);
            await ctx.Events.WaitAsync<LiveActivity>(TestLiveActivityDelegate.StartedTopic, TimeSpan.FromSeconds(5), ctx.CancellationToken, x => x.Id == activity.Id).ConfigureAwait(false);
            await ctx.WaitUntilAsync(() => activities.GetAll().Any(x => x.Id == activity.Id), TimeSpan.FromSeconds(10), "GetAll to list the new activity").ConfigureAwait(false);

            if (ctx.IsAndroid)
                Assert.Contains(await DeliveredNotifications.GetAsync().ConfigureAwait(false), x => x.ChannelId == AndroidChannelId);

            await activities.Update(activity.Id, new LiveActivityContent
            {
                Title = "Device test",
                Body = "Halfway",
                ShortStatus = "50%",
                Progress = LiveActivityProgress.FromValue(0.5)
            }, cancelToken: ctx.CancellationToken).ConfigureAwait(false);

            await activities.End(activity.Id, dismissAt: DateTimeOffset.UtcNow, cancelToken: ctx.CancellationToken).ConfigureAwait(false);
            await ctx.WaitUntilAsync(
                () => activities.GetAll().All(x => x.Id != activity.Id || x.State is LiveActivityState.Ended or LiveActivityState.Dismissed),
                TimeSpan.FromSeconds(10),
                "the activity to end"
            ).ConfigureAwait(false);

            if (ctx.IsAndroid)
            {
                await ctx.Events.WaitAsync<LiveActivity>(TestLiveActivityDelegate.StateTopic, TimeSpan.FromSeconds(5), ctx.CancellationToken, x => x.Id == activity.Id && x.State == LiveActivityState.Ended).ConfigureAwait(false);
                Assert.DoesNotContain(await DeliveredNotifications.GetAsync().ConfigureAwait(false), x => x.ChannelId == AndroidChannelId);
            }
        });
    }
}

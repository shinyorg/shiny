using Shiny.Notifications;
using Shiny.Push;

namespace Shiny.DeviceTests.Tests;


public class PushTests(IPushManager push, INotificationManager notifications) : DeviceTestSuite
{
    public override string Name => "Push";


    protected override void Define()
    {
        this.Test("GetCurrentAccess_DoesNotThrow", async ctx =>
        {
            var access = await push.GetCurrentAccess().ConfigureAwait(false);
            ctx.Log($"Push access: {access}");

            // documented: Shiny reports the iOS simulator as unsupported for push registration
            if (ctx.Platform == "ios" && ctx.IsSimulator)
                Assert.Equal(AccessState.NotSupported, access);
        });

        this.Test("RequestAccess_Simulator_ReportsNotSupported", async ctx =>
        {
            ctx.SkipUnless(ctx.Platform == "ios" && ctx.IsSimulator, "Real registration needs the aps-environment entitlement (iOS) or google-services.json (Android), which this app does not carry");

            var result = await push.RequestAccess(ctx.CancellationToken).ConfigureAwait(false);
            Assert.Equal(AccessState.NotSupported, result.Status);
            Assert.Null(result.RegistrationToken);
        });

        this.Test("AlertPush_Foreground_ReachesDelegate", async ctx =>
        {
            ctx.RequireCapability(HostCapability.Push);
            await EnsureNotificationAccess(ctx).ConfigureAwait(false);

            var nonce = Guid.NewGuid().ToString("N");
            await ctx.Host.SendPushAsync($$"""
                {
                    "aps": { "alert": { "title": "Device test", "body": "Alert push" }, "sound": "default" },
                    "devicetests": "{{nonce}}"
                }
                """, ctx.CancellationToken).ConfigureAwait(false);

            var received = await ctx.Events.WaitAsync<PushNotification>(
                TestPushDelegate.ReceivedTopic,
                TimeSpan.FromSeconds(20),
                ctx.CancellationToken,
                x => x.Data.TryGetValue("devicetests", out var v) && v == nonce
            ).ConfigureAwait(false);

            Assert.NotNull(received.Notification);
            Assert.Equal("Device test", received.Notification.Title);
            Assert.Equal("Alert push", received.Notification.Message);
        });

        this.Test("ImagePush_ReportsImageUri", async ctx =>
        {
            ctx.RequireCapability(HostCapability.Push);
            await EnsureNotificationAccess(ctx).ConfigureAwait(false);

            var (nonce, url) = await SendImagePush(ctx).ConfigureAwait(false);
            var received = await WaitForPush(ctx, nonce).ConfigureAwait(false);

            Assert.NotNull(received.Notification);
            Assert.Equal(url, received.Notification.ImageUri);
        });

        this.Test("ImagePush_ServiceExtension_AttachesImage", async ctx =>
        {
            ctx.SkipUnless(ctx.Platform == "ios", "The notification service extension is iOS only");
            // simctl push hands the payload to usernotificationsd as addNotificationRequest - the APNs path that
            // launches a service extension is never taken (verified: the extension process never starts)
            ctx.SkipUnless(!ctx.IsSimulator, "simctl push bypasses the APNs pipeline, so iOS never runs the notification service extension - needs a real APNs push");
            ctx.RequireCapability(HostCapability.Push);
            await EnsureNotificationAccess(ctx).ConfigureAwait(false);

            var (nonce, _) = await SendImagePush(ctx).ConfigureAwait(false);
            await WaitForPush(ctx, nonce).ConfigureAwait(false);

#if IOS
            var delivered = await DeliveredNotifications.WaitForPayloadAsync(ctx, "devicetests", nonce, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            Assert.NotEmpty(delivered.Request.Content.Attachments);
#endif
        }, TimeSpan.FromSeconds(60));

        this.Test("SilentPush_ReachesDelegate", async ctx =>
        {
            ctx.RequireCapability(HostCapability.Push);

            var nonce = Guid.NewGuid().ToString("N");
            try
            {
                await ctx.Host.SendPushAsync($$"""
                    {
                        "aps": { "content-available": 1 },
                        "devicetests": "{{nonce}}"
                    }
                    """, ctx.CancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("no user visible content", StringComparison.OrdinalIgnoreCase))
            {
                // simctl push rejects background pushes (UNErrorDomain 1401) - only a real device/APNs can deliver one
                TestContext.Skip("The simulator refuses silent pushes (simctl push: 'Notification has no user visible content')");
            }

            var received = await ctx.Events.WaitAsync<PushNotification>(
                TestPushDelegate.ReceivedTopic,
                TimeSpan.FromSeconds(20),
                ctx.CancellationToken,
                x => x.Data.TryGetValue("devicetests", out var v) && v == nonce
            ).ConfigureAwait(false);

            Assert.Null(received.Notification);
        });
    }


    static async Task<(string Nonce, string Url)> SendImagePush(TestContext ctx)
    {
        var nonce = Guid.NewGuid().ToString("N");
        var url = ctx.Host.Url($"files/image.png?id={nonce}");
        var payload = "{ \"aps\": { \"alert\": { \"title\": \"Device test\", \"body\": \"Image push\" }, \"mutable-content\": 1 }, "
            + $"\"image\": \"{url}\", \"devicetests\": \"{nonce}\" }}";

        await ctx.Host.SendPushAsync(payload, ctx.CancellationToken).ConfigureAwait(false);
        return (nonce, url);
    }


    static Task<PushNotification> WaitForPush(TestContext ctx, string nonce) => ctx.Events.WaitAsync<PushNotification>(
        TestPushDelegate.ReceivedTopic,
        TimeSpan.FromSeconds(30),
        ctx.CancellationToken,
        x => x.Data.TryGetValue("devicetests", out var v) && v == nonce
    );


    // a foreground alert push is only presented (and so only reaches OnReceived) once notifications are authorized
    async Task EnsureNotificationAccess(TestContext ctx)
    {
        var access = await notifications.RequestAccess().ConfigureAwait(false);
        ctx.SkipUnless(access == AccessState.Available, $"Notification permission is {access}");
    }
}

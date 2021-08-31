#if __IOS__
using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using CorePush.Apple;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shiny.Push;
using Shiny.Testing.Push;
using Xunit;
using Xunit.Abstractions;


namespace Shiny.Tests.Push
{
    public class AppleNativePushTests
    {
        readonly ITestOutputHelper output;
        readonly IPushManager push;
        readonly ApnSender apnSender;


        public AppleNativePushTests(ITestOutputHelper output)
        {
            this.output = output;
            this.apnSender = new ApnSender(
                new ApnSettings
                {
                    AppBundleIdentifier = TestStartup.CurrentPlatform.AppIdentifier, // com.shiny.test
                    ServerType = ApnServerType.Development,
                    P8PrivateKey = Secrets.Values.ApnPrivateKey,
                    P8PrivateKeyId = Secrets.Values.ApnPrivateKeyId,
                    TeamId = Secrets.Values.ApnTeamId
                },
                new System.Net.Http.HttpClient()
            );

            ShinyHost.Init(TestStartup.CurrentPlatform, new ActionStartup
            {
                BuildServices = x => x.UsePush<TestPushDelegate>(),
                BuildLogging = x => x.AddXUnit(output)
            });
            this.push = ShinyHost.Resolve<IPushManager>();
        }


        [Fact]
        public async Task EndToEnd()
        {
            var access = await this.push.RequestAccess();
            var task = this.push.WhenReceived().Take(1).Timeout(TimeSpan.FromSeconds(20)).ToTask();

            var response = await this.apnSender.SendAsync(
                new AppleNotification
                {
                    AlertBody = new AppleNotification.Alert
                    {
                        Title = "Test Title",
                        Body = "Test Body"
                    }
                },
                access.RegistrationToken
            );

            var result = await task.ConfigureAwait(false);
            result.Notification.Should().NotBeNull("Notification is null");
            result.Notification.Title.Should().Be("Test Title");
            result.Notification.Message.Should().Be("Test Body");
        }
    }


    public class AppleNotification
    {
        public class Alert
        {
            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("body")]
            public string Body { get; set; }
        }

        [JsonProperty("content-available")]
        public int ContentAvailable { get; set; } = 1;

        [JsonProperty("alert")]
        public Alert AlertBody { get; set; }

        [JsonProperty("apns-push-type")]
        public string PushType { get; set; } = "alert";
    }
}
#endif
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.NotificationHubs;
using Shiny.Push;
using Shiny.Testing.Push;
using Xunit;


namespace Shiny.Tests.Push
{
    public class AzureNotificationHubTests
    {
        private const string FcmSampleNotificationContent = "{\"data\":{\"message\":\"Notification Hub test notification from SDK sample\"}}";
        private const string FcmSampleSilentNotificationContent = "{ \"message\":{\"data\":{ \"Nick\": \"Mario\", \"body\": \"great match!\", \"Room\": \"PortugalVSDenmark\" } }}";
        private const string AppleSampleNotificationContent = "{\"aps\":{\"alert\":\"Notification Hub test notification from SDK sample\"}}";
        private const string AppleSampleSilentNotificationContent = "{\"aps\":{\"content-available\":1}, \"foo\": 2 }";
        private const string WnsSampleNotification = "<?xml version=\"1.0\" encoding=\"utf-8\"?><toast><visual><binding template=\"ToastText01\"><text id=\"1\">Notification Hub test notification from SDK sample</text></binding></visual></toast>";
        readonly IPushTagSupport pushManager;
        readonly NotificationHubClient hubClient;


        public AzureNotificationHubTests()
        {
            var cfg = global::Shiny.Tests.Secrets.Values;
            ShinyHost.Init(TestStartup.CurrentPlatform, new ActionStartup
            {
                BuildServices = (services) =>
                {
                    services.UsePushAzureNotificationHubs<TestPushDelegate>(
                        cfg.AzureNotificationHubListenerConnectionString,
                        cfg.AzureNotificationHubName
                    );
                }
            });
            this.pushManager = (IPushTagSupport)ShinyHost.Resolve<IPushManager>();
            this.hubClient = NotificationHubClient.CreateClientFromConnectionString(
                cfg.AzureNotificationHubFullConnectionString,
                cfg.AzureNotificationHubName
            );
        }


        [Fact]
        public Task Register_UnRegister() => this.WrapRegistration(async regToken =>
        {
            this.pushManager.CurrentRegistrationToken.Should().Be(regToken);
            this.pushManager.CurrentRegistrationTokenDate.Should().NotBeNull("TokenDate not set");
            this.pushManager.CurrentRegistrationExpiryDate.Should().NotBeNull("ExpiryDate not set");

            var install = await this.hubClient.GetInstallationAsync(regToken);
            install.Should().NotBeNull("Install was not found");

            await this.pushManager.UnRegister();
            this.pushManager.CurrentRegistrationToken.Should().BeNull("Reg Token is still set");
            this.pushManager.CurrentRegistrationTokenDate.Should().BeNull("Reg Token Date is still set");
            this.pushManager.CurrentRegistrationExpiryDate.Should().BeNull("Reg Expiry Date is still set");

            install = await this.hubClient.GetInstallationAsync(regToken);
            install.Should().BeNull("Install was not deleted");
        });


        [Fact]
        public Task ReceiveOnForegroundObservable() => this.WrapRegistration(async token =>
        {
            var task = this.pushManager
                .WhenReceived()
                .Take(1)
                .Timeout(TimeSpan.FromSeconds(10))
                .ToTask();

            await this.DoSend();

            await task;
        });


        [Fact]
        public Task ReceiveOnDelegate() => this.WrapRegistration(async token =>
        {
            var tcs = new TaskCompletionSource<object?>();
            TestPushDelegate.Received += data => tcs.SetResult(null);
            await this.DoSend();
            await tcs.Task.WithTimeout(10);
        });



        [Fact]
        public Task Tags() => this.WrapRegistration(async token =>
        {
            var random = Guid.NewGuid().ToString();
            await this.pushManager.AddTag(random + "1");
            await this.pushManager.AddTag(random + "2");
            await this.pushManager.AddTag(random + "3");

            var install = await this.hubClient.GetInstallationAsync(this.pushManager.CurrentRegistrationToken);
            install.Tags.Any(x => x.Equals(random + "1")).Should().BeTrue("tag1 not found");
            install.Tags.Any(x => x.Equals(random + "2")).Should().BeTrue("tag2 not found");
            install.Tags.Any(x => x.Equals(random + "3")).Should().BeTrue("tag3 not found");

            await this.pushManager.RemoveTag(random + "2");
            install = await this.hubClient.GetInstallationAsync(this.pushManager.CurrentRegistrationToken);

            install.Tags.Any(x => x.Equals(random + "1")).Should().BeTrue("tag1 not found");
            install.Tags.Any(x => x.Equals(random + "2")).Should().BeFalse("tag2 found, but should be deleted");
            install.Tags.Any(x => x.Equals(random + "3")).Should().BeTrue("tag3 not found");

            await this.pushManager.ClearTags();
            install = await this.hubClient.GetInstallationAsync(this.pushManager.CurrentRegistrationToken);
            install.Tags.Count.Should().Be(0, "There should be 0 tags on this install now");
        });


        async Task WrapRegistration(Func<string, Task> innerTask)
        {
            var result = await this.pushManager.RequestAccess();
            result.Assert();

            try
            {
                await innerTask(result.RegistrationToken).ConfigureAwait(false);
            }
            finally
            {
                await this.pushManager.UnRegister();
            }
        }


        async Task DoSend()
        {
            await this.hubClient.SendAppleNativeNotificationAsync(AppleSampleNotificationContent);
            await this.hubClient.SendFcmNativeNotificationAsync(FcmSampleNotificationContent);
        }
    }
}
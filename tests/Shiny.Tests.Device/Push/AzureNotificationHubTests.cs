using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;

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
            ShinyHost.Init(TestStartup.CurrentPlatform, new ActionStartup
            {
                BuildServices = (services) =>
                {
                    services.UsePushAzureNotificationHubs<TestPushDelegate>(
                        Secrets.Values.AzureNotificationHubListenerConnectionString,
                        Secrets.Values.AzureNotificationHubName
                    );
                }
            });
            this.pushManager = (IPushTagSupport)ShinyHost.Resolve<IPushManager>();
            this.hubClient = NotificationHubClient.CreateClientFromConnectionString(
                Secrets.Values.AzureNotificationHubFullConnectionString,
                Secrets.Values.AzureNotificationHubName
            );
        }


        [Fact(DisplayName = "Push - ANH Register/UnRegister")]
        public Task Register_UnRegister() => this.WrapRegistration(async regToken =>
        {
            this.pushManager.CurrentRegistrationToken.Should().Be(regToken);
            this.pushManager.CurrentRegistrationTokenDate.Should().NotBeNull("TokenDate not set");
            this.pushManager.CurrentRegistrationExpiryDate.Should().NotBeNull("ExpiryDate not set");

            // did it remove off the server?
            //var install = await this.hubClient.GetInstallationAsync(regToken);
            //install.Should().NotBeNull("Install was not found");

            await this.pushManager.UnRegister();
            this.pushManager.CurrentRegistrationToken.Should().BeNull("Reg Token is still set");
            this.pushManager.CurrentRegistrationTokenDate.Should().BeNull("Reg Token Date is still set");
            this.pushManager.CurrentRegistrationExpiryDate.Should().BeNull("Reg Expiry Date is still set");

            try
            {
                await this.hubClient.GetInstallationAsync(regToken);
                throw new ArgumentException("This should not have been reached");
            }
            catch (MessagingEntityNotFoundException)
            {
                // this is what we want
            }
        });


        [Fact(DisplayName = "Push - ANH Receive Observable")]
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


        [Fact(DisplayName = "Push - ANH Receive Delegate")]
        public Task ReceiveOnDelegate() => this.WrapRegistration(async token =>
        {
            var tcs = new TaskCompletionSource<object?>();
            TestPushDelegate.Received += data => tcs.SetResult(null);
            await this.DoSend();
            await tcs.Task.WithTimeout(10);
        });



        [Fact(DisplayName = "Push - ANH Tags")]
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
            await this.pushManager.UnRegister();
            var result = await this.pushManager.RequestAccess();
            result.Assert();
            await innerTask(result.RegistrationToken);
        }


        async Task DoSend()
        {
            var name = TestStartup.CurrentPlatform.GetType().Name;

            if (name.Equals("AndroidPlatform"))
            {
                await this.hubClient.SendFcmNativeNotificationAsync(FcmSampleNotificationContent);
            }
            else if (name.Equals("ApplePlatform"))
            {
                await this.hubClient.SendAppleNativeNotificationAsync(AppleSampleNotificationContent);
            }
            else if (name.Equals("UwpPlatform"))
            {
                await this.hubClient.SendWindowsNativeNotificationAsync(WnsSampleNotification);
            }
            else
            {
                throw new ArgumentException("Invalid Platform - " + name);
            }
        }
    }
}
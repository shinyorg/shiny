using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.NotificationHubs;
using Shiny.Infrastructure.DependencyInjection;
using Shiny.Push;
using Xunit;


namespace Shiny.Tests.Devices.Push
{
    public class AzureNotificationHubTests
    {
        private const string FcmSampleNotificationContent = "{\"data\":{\"message\":\"Notification Hub test notification from SDK sample\"}}";
        private const string FcmSampleSilentNotificationContent = "{ \"message\":{\"data\":{ \"Nick\": \"Mario\", \"body\": \"great match!\", \"Room\": \"PortugalVSDenmark\" } }}";
        private const string AppleSampleNotificationContent = "{\"aps\":{\"alert\":\"Notification Hub test notification from SDK sample\"}}";
        private const string AppleSampleSilentNotificationContent = "{\"aps\":{\"content-available\":1}, \"foo\": 2 }";
        private const string WnsSampleNotification = "<?xml version=\"1.0\" encoding=\"utf-8\"?><toast><visual><binding template=\"ToastText01\"><text id=\"1\">Notification Hub test notification from SDK sample</text></binding></visual></toast>";
        readonly Shiny.Push.AzureNotificationHubs.PushManager pushManager;
        NotificationHubClient hubClient;


        const string fullListenerConfig = "Endpoint=sb://shinysamples.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=8Vng3Iav9v4+YABrGJdWDlEuybsl2JbZm+xNalTqPq4=";
        const string listenOnlyListenerConfig = "Endpoint=sb://shinysamples.servicebus.windows.net/;SharedAccessKeyName=DefaultListenSharedAccessSignature;SharedAccessKey=jI6ss5WOD//xPNuHFJmS7sWWzqndYQyz7wAVOMTZoLE=";
        const string hubName = "shinysamples";


        public AzureNotificationHubTests()
        {
            ShinyHost.Init(TestStartup.CurrentPlatform, new ActionStartup
            {
                BuildServices = (services) =>
                {
                    services.UsePushAzureNotificationHubs<TestPushDelegate>(listenOnlyListenerConfig, hubName);
                }
            });

            this.pushManager = (Shiny.Push.AzureNotificationHubs.PushManager)ShinyHost.Resolve<IPushManager>();
            this.hubClient = NotificationHubClient.CreateClientFromConnectionString(fullListenerConfig, hubName);
        }


        [Fact]
        public async Task Register_UnRegister()
        {
            var result = await this.pushManager.RequestAccess();
            if (result.Status != AccessState.Available)
                throw new ArgumentException("Permission status failure - " + result.Status);

            this.pushManager.InstallationId.Should().Be(result.RegistrationToken);
            this.pushManager.NativeRegistrationToken.Should().NotBeNull();
            this.pushManager.CurrentRegistrationToken.Should().Be(result.RegistrationToken);
            this.pushManager.CurrentRegistrationTokenDate.Should().NotBeNull("TokenDate not set");
            this.pushManager.CurrentRegistrationExpiryDate.Should().NotBeNull("ExpiryDate not set");

            var install = await this.hubClient.GetInstallationAsync(result.RegistrationToken);
            install.Should().NotBeNull("Install was not found");

            await this.pushManager.UnRegister();
            this.pushManager.CurrentRegistrationToken.Should().BeNull("Reg Token is still set");
            this.pushManager.CurrentRegistrationTokenDate.Should().BeNull("Reg Token Date is still set");
            this.pushManager.CurrentRegistrationExpiryDate.Should().BeNull("Reg Expiry Date is still set");

            install = await this.hubClient.GetInstallationAsync(result.RegistrationToken);
            install.Should().BeNull("Install was not deleted");
        }


        //[Fact]
        //public async Task ReceiveNotification()
        //{
        //    await this.hubClient.SendAppleNativeNotificationAsync("jsonPayload", new[] { "tag" });
        //}


        [Fact]
        public async Task Tags()
        {
            await this.pushManager.RequestAccess();

            var random = Guid.NewGuid().ToString();
            await this.pushManager.AddTag(random + "1");
            await this.pushManager.AddTag(random + "2");
            await this.pushManager.AddTag(random + "3");

            var install = await this.hubClient.GetInstallationAsync(this.pushManager.InstallationId);
            install.Tags.Any(x => x.Equals(random + "1")).Should().BeTrue("tag1 not found");
            install.Tags.Any(x => x.Equals(random + "2")).Should().BeTrue("tag2 not found");
            install.Tags.Any(x => x.Equals(random + "3")).Should().BeTrue("tag3 not found");

            await this.pushManager.RemoveTag(random + "2");
            install = await this.hubClient.GetInstallationAsync(this.pushManager.InstallationId);

            install.Tags.Any(x => x.Equals(random + "1")).Should().BeTrue("tag1 not found");
            install.Tags.Any(x => x.Equals(random + "2")).Should().BeFalse("tag2 found, but should be deleted");
            install.Tags.Any(x => x.Equals(random + "3")).Should().BeTrue("tag3 not found");

            await this.pushManager.ClearTags();
            install = await this.hubClient.GetInstallationAsync(this.pushManager.InstallationId);
            install.Tags.Count.Should().Be(0, "There should be 0 tags on this install now");
        }



        //async Task SendNotification(string deviceId)
        //{

        //}
    }
}

/*

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;
using Microsoft.Extensions.Configuration;

namespace SendPushSample
{
    class Program
    {
        private const string FcmSampleNotificationContent = "{\"data\":{\"message\":\"Notification Hub test notification from SDK sample\"}}";
        private const string FcmSampleSilentNotificationContent = "{ \"message\":{\"data\":{ \"Nick\": \"Mario\", \"body\": \"great match!\", \"Room\": \"PortugalVSDenmark\" } }}";
        private const string AppleSampleNotificationContent = "{\"aps\":{\"alert\":\"Notification Hub test notification from SDK sample\"}}";
        private const string AppleSampleSilentNotificationContent = "{\"aps\":{\"content-available\":1}, \"foo\": 2 }";
        private const string WnsSampleNotification = "<?xml version=\"1.0\" encoding=\"utf-8\"?><toast><visual><binding template=\"ToastText01\"><text id=\"1\">Notification Hub test notification from SDK sample</text></binding></visual></toast>";

        static async Task Main(string[] args)
        {
            // Getting connection key from the new resource
            var config = LoadConfiguration(args);
            var nhClient = NotificationHubClient.CreateClientFromConnectionString(config.PrimaryConnectionString, config.HubName);

            // Register some fake devices
            var fcmDeviceId = Guid.NewGuid().ToString();
            var fcmInstallation = new Installation
            {
                InstallationId = "fake-fcm-install-id",
                Platform = NotificationPlatform.Fcm,
                PushChannel = fcmDeviceId,
                PushChannelExpired = false,
                Tags = new[] { "fcm" }
            };
            await nhClient.CreateOrUpdateInstallationAsync(fcmInstallation);

            var appleDeviceId = "00fc13adff785122b4ad28809a3420982341241421348097878e577c991de8f0";
            var apnsInstallation = new Installation
            {
                InstallationId = "fake-apns-install-id",
                Platform = NotificationPlatform.Apns,
                PushChannel = appleDeviceId,
                PushChannelExpired = false,
                Tags = new[] { "apns" }
            };
            await nhClient.CreateOrUpdateInstallationAsync(apnsInstallation);

            switch ((SampleConfiguration.Operation)Enum.Parse(typeof(SampleConfiguration.Operation), config.SendType))
            {
                case SampleConfiguration.Operation.Broadcast:
                    // Notification groups should be created on client side
                    var outcomeFcm = await nhClient.SendFcmNativeNotificationAsync(FcmSampleNotificationContent);
                    await GetPushDetailsAndPrintOutcome("FCM", nhClient, outcomeFcm);

                    var outcomeSilentFcm = await nhClient.SendFcmNativeNotificationAsync(FcmSampleSilentNotificationContent);
                    await GetPushDetailsAndPrintOutcome("FCM Silent", nhClient, outcomeSilentFcm);

                    // Send groupable notifications to iOS
                    var notification = new AppleNotification(AppleSampleNotificationContent);
                    if (!string.IsNullOrEmpty(config.AppleGroupId))
                    {
                        notification.Headers.Add("apns-collapse-id", config.AppleGroupId);
                    }

                    var outcomeApns = await nhClient.SendNotificationAsync(notification);
                    await GetPushDetailsAndPrintOutcome("APNS", nhClient, outcomeApns);

                    var outcomeSilentApns = await nhClient.SendAppleNativeNotificationAsync(AppleSampleSilentNotificationContent);
                    await GetPushDetailsAndPrintOutcome("APNS Silent", nhClient, outcomeSilentApns);

                    var outcomeWns = await nhClient.SendWindowsNativeNotificationAsync(WnsSampleNotification);
                    await GetPushDetailsAndPrintOutcome("WNS", nhClient, outcomeWns);

                    break;
                case SampleConfiguration.Operation.SendByTag:
                    // Send notifications by tag
                    var outcomeFcmByTag = await nhClient.SendFcmNativeNotificationAsync(FcmSampleNotificationContent, config.Tag ?? "fcm");
                    await GetPushDetailsAndPrintOutcome("FCM Tags", nhClient, outcomeFcmByTag);

                    var outcomeApnsByTag = await nhClient.SendAppleNativeNotificationAsync(AppleSampleNotificationContent, config.Tag ?? "apns");
                    await GetPushDetailsAndPrintOutcome("APNS Tags", nhClient, outcomeApnsByTag);

                    break;
                case SampleConfiguration.Operation.SendByDevice:
                    // Send notifications by deviceId
                    var outcomeFcmByDeviceId = await nhClient.SendDirectNotificationAsync(CreateFcmNotification(), config.FcmDeviceId ?? fcmDeviceId);
                    await GetPushDetailsAndPrintOutcome("FCM Direct", nhClient, outcomeFcmByDeviceId);

                    var outcomeApnsByDeviceId = await nhClient.SendDirectNotificationAsync(CreateApnsNotification(), config.AppleDeviceId ?? appleDeviceId);
                    await GetPushDetailsAndPrintOutcome("APNS Direct", nhClient, outcomeApnsByDeviceId);

                    break;
                default:
                    Console.WriteLine("Invalid Sendtype");
                    break;
            }
        }

        private static Notification CreateFcmNotification()
        {
            return new FcmNotification(FcmSampleNotificationContent);
        }

        private static Notification CreateApnsNotification()
        {
            return new AppleNotification(AppleSampleNotificationContent);
        }

        private static async Task<NotificationDetails> WaitForThePushStatusAsync(string pnsType, NotificationHubClient nhClient, NotificationOutcome notificationOutcome)
        {
            var notificationId = notificationOutcome.NotificationId;
            var state = NotificationOutcomeState.Enqueued;
            var count = 0;
            NotificationDetails outcomeDetails = null;
            while ((state == NotificationOutcomeState.Enqueued || state == NotificationOutcomeState.Processing) && ++count < 10)
            {
                try
                {
                    Console.WriteLine($"{pnsType} status: {state}");
                    outcomeDetails = await nhClient.GetNotificationOutcomeDetailsAsync(notificationId);
                    state = outcomeDetails.State;
                }
                catch (MessagingEntityNotFoundException)
                {
                    // It's possible for the notification to not yet be enqueued, so we may have to swallow an exception
                    // until it's ready to give us a new state.
                }
                Thread.Sleep(1000);
            }
            return outcomeDetails;
        }

        private static void PrintPushOutcome(string pnsType, NotificationDetails details, NotificationOutcomeCollection collection)
        {
            if (collection != null)
            {
                Console.WriteLine($"{pnsType} outcome: " + string.Join(",", collection.Select(kv => $"{kv.Key}:{kv.Value}")));
            }
            else
            {
                Console.WriteLine($"{pnsType} no outcomes.");
            }
            Console.WriteLine($"{pnsType} error details URL: {details.PnsErrorDetailsUri}");
        }

        private static void PrintPushNoOutcome(string pnsType)
        {
            Console.WriteLine($"{pnsType} has no outcome due to it is only available for Standard SKU pricing tier.");
        }

        private static SampleConfiguration LoadConfiguration(string[] args)
        {
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("config.json", true);
            configurationBuilder.AddCommandLine(args);
            var configRoot = configurationBuilder.Build();
            var sampleConfig = new SampleConfiguration();
            configRoot.Bind(sampleConfig);
            return sampleConfig;
        }

        private static async Task GetPushDetailsAndPrintOutcome(
            string pnsType,
            NotificationHubClient nhClient,
            NotificationOutcome notificationOutcome)
        {
            // The Notification ID is only available for Standard SKUs. For Basic and Free SKUs the API to get notification outcome details can not be called.
            if (string.IsNullOrEmpty(notificationOutcome.NotificationId))
            {
                PrintPushNoOutcome(pnsType);
                return;
            }

            var details = await WaitForThePushStatusAsync(pnsType, nhClient, notificationOutcome);
            NotificationOutcomeCollection collection = null;
            switch (pnsType)
            {
                case "FCM":
                case "FCM Silent":
                case "FCM Tags":
                case "FCM Direct":
                    collection = details.FcmOutcomeCounts;
                    break;

                case "APNS":
                case "APNS Silent":
                case "APNS Tags":
                case "APNS Direct":
                    collection = details.ApnsOutcomeCounts;
                    break;

                case "WNS":
                    collection = details.WnsOutcomeCounts;
                    break;
                default:
                    Console.WriteLine("Invalid Sendtype");
                    break;
            }

            PrintPushOutcome(pnsType, details, collection);
        }
    }
}
 */
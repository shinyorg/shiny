#if DEVICE_TESTS && !WINDOWS_UWP && OTHER_PUSH
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using OneSignal;
using Shiny.Push;
using Shiny.Testing.Push;
using Xunit;
using Xunit.Abstractions;


namespace Shiny.Tests.Push
{
    public class OneSignalTests
    {
        readonly NotificationService service;
        readonly IPushPropertySupport push;


        public OneSignalTests(ITestOutputHelper output)
        {
            OneSignalConfiguration.SetupApiKey(Secrets.Values.OneSignalRestApiKey);
            OneSignalConfiguration.SetupAppId(Secrets.Values.OneSignalAppId);
            this.service = new NotificationService();

            ShinyHost.Init(TestStartup.CurrentPlatform, new ActionStartup
            {
                BuildServices = x =>
                {
                    x.UseOneSignalPush<TestPushDelegate>(Secrets.Values.OneSignalAppId);
                },
                BuildLogging = x => x.AddXUnit(output)
            });
            this.push = (IPushPropertySupport)ShinyHost.Resolve<IPushManager>();
        }


        [Fact(DisplayName = "Push - OneSignal - E2E")]
        public async Task E2e()
        {
            var access = await this.push.RequestAccess();
            access.Status.Should().Be(AccessState.Available);
            access.RegistrationToken.Should().NotBeNull("Registration token is null");
            this.push.SetProperty("UserId", access.RegistrationToken!);
            var testValue = Guid.NewGuid().ToString();

            var pushTask = this.push
                .WhenReceived()
                .Timeout(TimeSpan.FromSeconds(10))
                .Take(1)
                .ToTask();

            var response = await this.service.CreateAsync(new CreateNotificationOptions
            {
                Contents = new Dictionary<string, string>
                {
                    { LanguageCode.English, "Test" }
                },
                Headings = new Dictionary<string, string>
                {
                    { LanguageCode.English, "TestHeading" }
                },
                Subtitle = new Dictionary<string, string>
                {
                    { LanguageCode.English, "TestSubtitle" }
                },
                IncludeExternalUserIds = new List<string>
                {
                    access.RegistrationToken
                },
                Data = new Dictionary<string, object>
                {
                    { "test", testValue }
                }
            });
            if (response.Errors?.ErrorMessages?.Any() ?? false)
            {
                var err = String.Concat(response.Errors.ErrorMessages, ", ");
                err.Should().BeNull();
            }

            var result = await pushTask.ConfigureAwait(false);
            result.Data.ContainsKey("test").Should().BeTrue();
            result.Data["test"].Should().Be(testValue);
        }
    }
}
#endif
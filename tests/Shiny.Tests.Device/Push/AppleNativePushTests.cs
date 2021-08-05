using System;
using System.Threading.Tasks;
using CorePush.Apple;
using Microsoft.Extensions.Logging;
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
            //services.AddHttpClient<ApnSender>();
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
        public async Task Test()
        {
            var token = await this.push.RequestAccess();

            //this.apnSender.SendAsync("")
        }
    }
}

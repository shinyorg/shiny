//#if __IOS__
//using System;
//using System.Reactive.Linq;
//using System.Reactive.Threading.Tasks;
//using System.Threading.Tasks;
//using CorePush.Apple;
//using FluentAssertions;
//using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
//using Shiny.Push;
//using Shiny.Testing.Push;
//using Xunit;
//using Xunit.Abstractions;


//namespace Shiny.Tests.Push
//{
//    public class AppleNativeTests
//    {
//        readonly IPushManager push;
//        readonly ApnSender apnSender;


//        public AppleNativeTests(ITestOutputHelper output)
//        {
//            this.apnSender = new ApnSender(
//                new ApnSettings
//                {
//                    AppBundleIdentifier = TestStartup.CurrentPlatform.AppIdentifier, // com.shiny.test
//                    ServerType = ApnServerType.Development,
//                    P8PrivateKey = Secrets.Values.ApnPrivateKey,
//                    P8PrivateKeyId = Secrets.Values.ApnPrivateKeyId,
//                    TeamId = Secrets.Values.ApnTeamId
//                },
//                new System.Net.Http.HttpClient()
//            );

//            ShinyHost.Init(TestStartup.CurrentPlatform, new ActionStartup
//            {
//                BuildServices = x => x.UsePush<TestPushDelegate>(),
//                BuildLogging = x => x.AddXUnit(output)
//            });
//            this.push = ShinyHost.Resolve<IPushManager>();
//        }



//    }


//    public class AppleNotification
//    {
//        public class Alert
//        {
//            [JsonProperty("title")]
//            public string Title { get; set; }

//            [JsonProperty("body")]
//            public string Body { get; set; }
//        }

//        [JsonProperty("content-available")]
//        public int ContentAvailable { get; set; } = 1;

//        [JsonProperty("alert")]
//        public Alert AlertBody { get; set; }

//        [JsonProperty("apns-push-type")]
//        public string PushType { get; set; } = "alert";
//    }
//}
//#endif
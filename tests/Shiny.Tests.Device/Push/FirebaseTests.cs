using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Shiny.Push;
using Shiny.Testing.Push;
using Xunit;


namespace Shiny.Tests.Push
{
    public class FirebaseTests
    {
        readonly IPushTagSupport pushManager;
        readonly FirebaseApp firebase;
        readonly global::FirebaseAdmin.Messaging.FirebaseMessaging messaging;


        public FirebaseTests()
        {
            ShinyHost.Init(TestStartup.CurrentPlatform, new ActionStartup
            {
                BuildServices = (services) =>
                {
                    services.UseFirebaseMessaging<TestPushDelegate>();
                }
            });

            this.pushManager = (IPushTagSupport)ShinyHost.Resolve<IPushManager>();

            this.firebase = FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromAccessToken("")
            });
            this.messaging = global::FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance;
        }


        [Fact]
        public Task ReceiveTopic() => this.DoSend(Guid.NewGuid().ToString());


        [Fact]
        public Task ReceviedOnForegroundObservable() => this.DoSend(null);


        [Fact]
        public async Task ReceivedOnDelegate()
        {
            var stamp = Guid.NewGuid().ToString();
            var result = await this.pushManager.RequestAccess();
            var tcs = new TaskCompletionSource<object>();

            TestPushDelegate.Received = (data) =>
            {
                if (data.ContainsKey("stamp") && data["stamp"] == stamp)
                    tcs.SetResult(null);
            };
            await this.messaging.SendAsync(new Message
            {
                Token = result.RegistrationToken,
                Data = new Dictionary<string, string>
                {
                    { "stamp", stamp }
                }
            });
            await tcs.Task.WithTimeout(10);
        }


        async Task DoSend(string topic)
        {
            var stamp = Guid.NewGuid().ToString();
            var result = await this.pushManager.RequestAccess();

            var task = this.pushManager
                .WhenReceived()
                .Take(1)
                .Where(x => x.ContainsKey("stamp") && x["stamp"] == stamp)
                .Timeout(TimeSpan.FromSeconds(5))
                .ToTask();

            var msg = new Message
            {
                Token = result.RegistrationToken,
                Data = new Dictionary<string, string>
                {
                    { "stamp", stamp }
                }
            };
            if (!topic.IsEmpty())
            {
                msg.Topic = topic;
                await this.pushManager.AddTag(topic);
            }
            await this.messaging.SendAsync(msg);

            await task; // wait for message

            if (!topic.IsEmpty())
                await this.pushManager.ClearTags();
        }
    }
}

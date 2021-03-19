using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Shiny.Push;
using Shiny.Testing.Push;
using Xunit;


namespace Shiny.Tests.Push
{
    public class FirebaseTests : IDisposable
    {
        readonly IPushTagSupport pushManager;



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
            
            var creds = GoogleCredential.FromJson(JsonConvert.SerializeObject(new TestGoogleCredential
            {
                ProjectId = Secrets.Values.GoogleCredentialProjectId,
                PrivateKeyId = Secrets.Values.GoogleCredentialPrivateKeyId,
                PrivateKey = Secrets.Values.GoogleCredentialPrivateKey,
                ClientId = Secrets.Values.GoogleCredentialClientId,
                ClientEmail = Secrets.Values.GoogleCredentialClientEmail,
                ClientCertUrl = Secrets.Values.GoogleCredentialClientCertUrl
            }));
            FirebaseApp.Create(new AppOptions
            {
                Credential = creds
            });
        }


        public void Dispose() => FirebaseApp.DefaultInstance.Delete();


        [Fact(DisplayName = "Push - Firebase - Receive Topic")]
        public Task ReceiveTopic() => this.DoSend(Guid.NewGuid().ToString());


        [Fact(DisplayName = "Push - Firebase - Observable")]
        public async Task ReceviedOnForegroundObservable()
        {
            var task = this.pushManager
                .WhenReceived()
                .Take(1)
                .Where(x => x.ContainsKey("stamp") && x["stamp"] == stamp)
                .Timeout(TimeSpan.FromSeconds(5))
                .ToTask();

            await this.DoSend(null);
            await task;
        }


        [Fact(DisplayName = "Push - Firebase - Delegate")]
        public async Task ReceivedOnDelegate()
        {
            var stamp = Guid.NewGuid().ToString();
            var result = await this.pushManager.RequestAccess();
            var tcs = new TaskCompletionSource<object>();

            TestPushDelegate.Received = (data) =>
            {
                if (data.ContainsKey("stamp") && data["stamp"] == stamp)
                    tcs?.SetResult(null);
            };
            await FirebaseMessaging.DefaultInstance.SendAsync(new Message
            {
                Token = result.RegistrationToken,
                Data = new Dictionary<string, string>
                {
                    { "stamp", stamp }
                }
            });
            await tcs.Task.WithTimeout(10);
            tcs = null;
        }


        async Task DoSend(string topic)
        {
            var stamp = Guid.NewGuid().ToString();
            var result = await this.pushManager.RequestAccess();

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
            await FirebaseMessaging.DefaultInstance.SendAsync(msg);

            if (!topic.IsEmpty())
                await this.pushManager.ClearTags();
        }
    }
}

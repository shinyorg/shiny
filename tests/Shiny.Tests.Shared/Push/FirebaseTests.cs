#if DEVICE_TESTS && !WINDOWS_UWP && FIREBASE
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shiny.Push;
using Shiny.Testing.Push;
using Xunit;
using Xunit.Abstractions;


namespace Shiny.Tests.Push
{
    public class FirebaseTests : IDisposable
    {
        const int TIMEOUT_SECS = 20;
        readonly ITestOutputHelper output;
        readonly IPushTagSupport pushManager;


        public FirebaseTests(ITestOutputHelper output)
        {
            this.output = output;
            ShinyHost.Init(TestStartup.CurrentPlatform, new ActionStartup
            {
                BuildServices = x => x.UseFirebaseMessaging<TestPushDelegate>(
                    //new FirebaseConfiguration(
                        //"1:29585461192:ios:fd58dc671e66c940313afd",
                        //"29585461192",
                        //"AIzaSyBQx5SBjutEx4VQUs5_ZDEzFsXPBPnf9tw"
                    //)
                ),
                BuildLogging = x => x.AddXUnit(output)
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
        public Task ReceiveTopic() => this.WrapRegister(async () =>
        {
            var stamp = Guid.NewGuid().ToString();
            var task = this.ListenForStamp(stamp);

            await this.DoSend(stamp, stamp);
            await task.WithTimeout(TIMEOUT_SECS);
        });


        [Fact(DisplayName = "Push - Firebase - Observable")]
        public Task ReceviedOnForegroundObservable() => this.WrapRegister(async () =>
        {
            var stamp = Guid.NewGuid().ToString();
            var task = this.ListenForStamp(stamp);

            await this.DoSend(stamp, null);
            await task.WithTimeout(TIMEOUT_SECS);
        });


        [Fact(DisplayName = "Push - Firebase - Delegate")]
        public Task ReceivedOnDelegate() => WrapRegister(async () =>
        {
            var stamp = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<object?>();

            TestPushDelegate.Receive = pr =>
            {
                if (pr.Data.ContainsKey("stamp") && pr.Data["stamp"] == stamp)
                    tcs?.SetResult(null);
            };
            await this.DoSend(stamp, null);
            await tcs.Task.WithTimeout(TIMEOUT_SECS);
            tcs = null;
        });



        Task ListenForStamp(string stamp) => this.pushManager
            .WhenReceived()
            .Take(1)
            .Where(x => x.Data.ContainsKey(stamp) && x.Data[stamp] == stamp)
            .ToTask();


        async Task WrapRegister(Func<Task> innerTask)
        {

            await this.pushManager.RequestAccess();

            try
            {
                await innerTask();
            }
            finally
            {
                await this.pushManager.UnRegister();
            }

        }


        async Task DoSend(string stamp, string? topic)
        {
            var msg = new Message
            {
                Data = new Dictionary<string, string>
                {
                    { "stamp", stamp }
                },
                Apns = new ApnsConfig
                {
                    Aps = new Aps
                    {
                        ContentAvailable = true
                    }
                }
            };

            if (topic.IsEmpty())
            {
                msg.Token = this.pushManager.CurrentRegistrationToken;
            }
            else
            {
                await this.pushManager.AddTag(topic!);
                msg.Topic = topic;
            }
            var response = await FirebaseMessaging.DefaultInstance.SendAsync(msg);
            this.output.WriteLine("Firebase Response: " + response);
        }
    }
}
#endif
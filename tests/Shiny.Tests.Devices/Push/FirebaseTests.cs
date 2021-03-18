using System;
using Shiny.Push;

namespace Shiny.Tests.Devices.Push
{
    public class FirebaseTests
    {
        readonly Shiny.Push.FirebaseMessaging.PushManager pushManager;


        public FirebaseTests()
        {
            ShinyHost.Init(TestStartup.CurrentPlatform, new ActionStartup
            {
                BuildServices = (services) =>
                {
                    services.UseFirebaseMessaging<TestPushDelegate>();
                }
            });

            this.pushManager = (Shiny.Push.FirebaseMessaging.PushManager)ShinyHost.Resolve<IPushManager>();
            //new FirebaseMessagingService();
        }
    }
}

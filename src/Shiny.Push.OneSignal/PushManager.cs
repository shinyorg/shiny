using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Settings;
using OS = global::Com.OneSignal.OneSignal;
using Com.OneSignal.Abstractions;


namespace Shiny.Push.OneSignal
{
    public class PushManager : AbstractPushManager, IPushTagSupport, IShinyStartupTask
    {
        public PushManager(ISettings settings) : base(settings)
        {
        }


        public void Start()
        {
            OS.Current.SetLogLevel(LOG_LEVEL.ERROR, LOG_LEVEL.NONE);
            OS.Current
                .StartInit("")
                .Settings(new Dictionary<string, bool>
                {
                    { IOSSettings.kOSSettingsKeyAutoPrompt, false },
                    { IOSSettings.kOSSettingsKeyInAppLaunchURL, false }
                })
                .InFocusDisplaying(OSInFocusDisplayOption.Notification)
                .EndInit();

            //// The promptForPushNotificationsWithUserResponse function will show the iOS push notification prompt. We recommend removing the following code and instead using an In-App Message to prompt for notification permission (See step 7)

        }


        public override Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            OS.Current.RegisterForPushNotifications();
            //OS.Current.SendTags()
            return Task.FromResult(PushAccessState.Denied);
        }


        public override Task UnRegister()
        {
            throw new NotImplementedException();
        }


        public override IObservable<IDictionary<string, string>> WhenReceived()
        {
            throw new NotImplementedException();
        }


        public Task SetTags(params string[] tags)
        {
            //OS.Current.DeleteTags()
            //OS.Current.SendTags()
            throw new NotImplementedException();
        }
    }
}
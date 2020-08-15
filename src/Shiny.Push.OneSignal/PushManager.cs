using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Settings;
using OS = global::Com.OneSignal.OneSignal;
using Com.OneSignal.Abstractions;
using System.Reactive.Subjects;

namespace Shiny.Push.OneSignal
{
    public class PushManager : AbstractPushManager, IPushTagSupport, IShinyStartupTask
    {
        readonly OneSignalPushConfig config;
        readonly IServiceProvider services;
        readonly Subject<IDictionary<string, string>> receivedSubj;


        public PushManager(OneSignalPushConfig config, ISettings settings, IServiceProvider services) : base(settings)
        {
            this.receivedSubj = new Subject<IDictionary<string, string>>();
            this.config = config;
            this.services = services;
        }


        public void Start()
        {
            OS.Current.SetLogLevel(this.config.LogLevel, this.config.VisualLogLevel);
            OS.Current
                .StartInit(this.config.AppId)
                .HandleNotificationOpened(async x =>
                {
                    //x.action.type == OSNotificationAction.ActionType.
                    var data = x.notification.payload.ToDictionary();
                    var args = new PushEntryArgs(null, x.action.actionID, null, data);
                    await this.services.RunDelegates<IPushDelegate>(x => x.OnEntry(args));
                })
                .HandleNotificationReceived(async x =>
                {
                    var data = x.payload.ToDictionary();
                    await this.services.RunDelegates<IPushDelegate>(x => x.OnReceived(data));
                    this.receivedSubj.OnNext(data);
                })
                .Settings(new Dictionary<string, bool>
                {
                    { IOSSettings.kOSSettingsKeyAutoPrompt, false },
                    { IOSSettings.kOSSettingsKeyInAppLaunchURL, false }
                })
                .InFocusDisplaying(this.config.InFocusDisplay)
                .EndInit();

            //OS.Current.IdsAvailable(x =>
            //{

            //});

            //// The promptForPushNotificationsWithUserResponse function will show the iOS push notification prompt. We recommend removing the following code and instead using an In-App Message to prompt for notification permission (See step 7)

        }


        public override async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            OS.Current.RegisterForPushNotifications();
            var ids = await OS.Current.IdsAvailableAsync();
            //OS.Current.SendTags()
            return PushAccessState.Denied;
        }


        public override Task UnRegister()
        {
            throw new NotImplementedException();
        }


        public override IObservable<IDictionary<string, string>> WhenReceived() => this.receivedSubj;


        public Task SetTags(params string[] tags)
        {
            if (!this.RegisteredTags.IsEmpty())
                OS.Current.DeleteTags(this.RegisteredTags.ToList());

            //OS.Current.SendTags(tags.ToList());
            this.RegisteredTags = tags;
            return Task.CompletedTask;
        }
    }
}
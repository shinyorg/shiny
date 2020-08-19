using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Settings;
using System.Reactive.Subjects;
using OS = global::Com.OneSignal.OneSignal;
using Com.OneSignal.Abstractions;


namespace Shiny.Push.OneSignal
{
    public class PushManager : AbstractPushManager, IPushTagSupport, IShinyStartupTask
    {
        readonly OneSignalPushConfig config;
        readonly IServiceProvider services;
        readonly Subject<IDictionary<string, string>> receivedSubj;


        public PushManager(OneSignalPushConfig config,
                           ISettings settings,
                           IServiceProvider services) : base(settings)
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
                    var data = ToDictionary(x.notification.payload);
                    var args = new PushEntryArgs("onesignal", x.action.actionID, null, data);
                    await this.services.RunDelegates<IPushDelegate>(x => x.OnEntry(args));
                })
                .HandleNotificationReceived(async x =>
                {
                    var data = ToDictionary(x.payload);
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
        }


        public override async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            OS.Current.SetSubscription(true);
            OS.Current.RegisterForPushNotifications();
            var ids = await OS.Current.IdsAvailableAsync();
            //OS.Current.SendTags()
            return PushAccessState.Denied;
        }


        public override Task UnRegister()
        {
            OS.Current.SetSubscription(false);
            return Task.CompletedTask;
        }


        public override IObservable<IDictionary<string, string>> WhenReceived() => this.receivedSubj;


        public Task SetTags(params string[] tags)
        {
            if (!this.RegisteredTags.IsEmpty())
                OS.Current.DeleteTags(this.RegisteredTags.ToList());

            if ((tags?.Length ?? 0) > 0)
            {
                OS.Current.SendTags(tags.ToDictionary(
                    x => x,
                    _ => "1"
                ));
            }
            this.RegisteredTags = tags;
            return Task.CompletedTask;
        }


        static IDictionary<string, string> ToDictionary(OSNotificationPayload payload)
            => payload?
                .additionalData?
                .ToDictionary(
                    y => y.Key,
                    y => y.Value.ToString()
                )
                ?? new Dictionary<string, string>(0);
    }
}
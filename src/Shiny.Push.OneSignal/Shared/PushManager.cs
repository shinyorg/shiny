#if !NETSTANDARD
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using Com.OneSignal.Abstractions;
using OS = global::Com.OneSignal.OneSignal;
using Shiny.Infrastructure;
using Shiny.Notifications;


namespace Shiny.Push.OneSignal
{
    public class PushManager : IPushManager,
                               IPushPropertySupport,
                               IShinyStartupTask
    {
        readonly OneSignalPushConfig config;
        readonly ShinyCoreServices core;
        readonly Subject<PushNotification> receivedSubj;


        public PushManager(OneSignalPushConfig config,
                           ShinyCoreServices core)
        {
            this.receivedSubj = new Subject<PushNotification>();
            this.config = config;
            this.core = core;
        }


        public void Start()
        {
            OS.Current.SetLogLevel(this.config.LogLevel, this.config.VisualLogLevel);
            OS.Current
                .StartInit(this.config.AppId)
                .HandleNotificationOpened(async x =>
                {
                    var notification = ToNotification(x.notification?.payload);
                    var pr = new PushNotificationResponse(notification, x.action?.actionID, null);
                    await this.core.Services.RunDelegates<IPushDelegate>(x => x.OnEntry(pr));
                })
                .HandleNotificationReceived(async x =>
                {
                    // silence onesignal notification
                    x.silentNotification = true;

                    // could also build channel here
                    var notification = ToNotification(x.payload);
                    var pn = new PushNotification(notification.Payload, notification);
                    await this.core.Services.RunDelegates<IPushDelegate>(x => x.OnReceived(pn));
                    this.receivedSubj.OnNext(pn);
                })
                .Settings(new Dictionary<string, bool>
                {
                    { IOSSettings.kOSSettingsKeyAutoPrompt, config.iOSAutoPrompt },
                    { IOSSettings.kOSSettingsKeyInAppLaunchURL, config.iOSInAppLaunchURL }
                })
                .InFocusDisplaying(this.config.InFocusDisplay)
                .EndInit();
        }


        public override async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            OS.Current.SetSubscription(true);
            OS.Current.RegisterForPushNotifications();
            var ids = await OS.Current.IdsAvailableAsync();
            return new PushAccessState(AccessState.Available, ids.PushToken);
        }


        public override Task UnRegister()
        {
            OS.Current.SetSubscription(false);
            return Task.CompletedTask;
        }


        public override IObservable<PushNotification> WhenReceived() => this.receivedSubj;
        public IReadOnlyDictionary<string, string> CurrentProperties => this.Properties;


        public void ClearProperties()
        {
            OS.Current.RemoveExternalUserId();
            OS.Current.SetLocationShared(false);
            OS.Current.DeleteTags(this.CurrentProperties.Keys.ToList());
            this.Properties.Clear();
            this.WriteProps();
        }


        public void RemoveProperty(string property)
        {
            switch (property.ToLower())
            {
                case "userid":
                    OS.Current.RemoveExternalUserId();
                    break;

                case "location":
                    OS.Current.SetLocationShared(false);
                    break;

                default:
                    OS.Current.DeleteTag(property);
                    break;
            }
            this.Properties.Remove(property);
            this.WriteProps();
        }


        public void SetProperty(string property, string value)
        {
            switch (property.ToLower())
            {
                case "userid":
                    OS.Current.SetExternalUserId(value);
                    break;

                case "location":
                    OS.Current.SetLocationShared(value.Equals("true", StringComparison.CurrentCultureIgnoreCase));
                    break;

                default:
                    OS.Current.SendTag(property, value);
                    break;
            }
            this.Properties[property] = value;
            this.WriteProps();
        }


        static Notification ToNotification(OSNotificationPayload payload)
        {
            var data = payload?
                .additionalData?
                .ToDictionary(
                    y => y.Key,
                    y => y.Value.ToString()
                )
                ?? new Dictionary<string, string>(0);

            return new Notification
            {
                Title = payload?.title,
                Message = payload?.body,
                BadgeCount = payload?.badge,
                Payload = data
            };
        }


        Dictionary<string, string>? props;
        Dictionary<string, string> Properties
        {
            get
            {
                this.props ??= this.Services
                    .Settings
                    .Get<Dictionary<string, string>>(nameof(this.Properties))
                    ?? new Dictionary<string, string>(0);
                return props;
            }
        }


        void WriteProps() => this.Services.Settings.Set(
            nameof(this.Properties),
            this.Properties
        );
    }
}
#endif
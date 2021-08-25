using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Firebase.CloudMessaging;
using Firebase.InstanceID;
using Microsoft.Extensions.Logging;
using Shiny.Push.Infrastructure;


namespace Shiny.Push.FirebaseMessaging
{
    public class PushManager : IPushManager, IPushTagSupport
    {
        readonly INativeAdapter adapter;
        readonly PushContainer container;
        readonly ILogger logger;
        readonly FirebaseConfiguration? config;


        public PushManager(INativeAdapter adapter,
                           PushContainer container,
                           ILogger<PushManager> logger,
                           FirebaseConfiguration? config = null)
        {
            this.adapter = adapter;
            this.container = container;
            this.logger = logger;
            this.config = config;
        }


        public DateTime? CurrentRegistrationTokenDate => this.container.CurrentRegistrationTokenDate;
        public string? CurrentRegistrationToken => this.container.CurrentRegistrationToken;
        public string[]? RegisteredTags => this.container.RegisteredTags;
        public IObservable<PushNotification> WhenReceived() => this.container.WhenReceived();


        public void Start()
        {
            if (this.CurrentRegistrationToken != null)
            {
                try
                {
                    this.TryStartFirebase();
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning("Failed to start Firebase Push", ex);
                }
            }
        }


        protected virtual void TryStartFirebase()
        {
            if (Messaging.SharedInstance == null)
            {
                if (this.config == null)
                {
                    Firebase.Core.App.Configure();
                    Messaging.SharedInstance!.AutoInitEnabled = true;
                }
                else
                {
                    Firebase.Core.App.Configure(new Firebase.Core.Options(
                        this.config.AppId,
                        this.config.ProjectId
                    ));
                }
            }
            Messaging.SharedInstance!.Delegate = new FbMessagingDelegate
            (
                async msg =>
                {
                    // I can't get access to the notification here
                    var dict = msg.AppData.FromNsDictionary();
                    var pr = new PushNotification(dict, null);
                    await this.container.OnReceived(pr).ConfigureAwait(false);
                },
                async token =>
                {
                    this.container.SetCurrentToken(token, true);
                    await this.container.OnTokenRefreshed(token).ConfigureAwait(false);
                }
            );
        }


        public async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var result = await this.adapter.RequestAccess().ConfigureAwait(false);
            this.TryStartFirebase();
            Messaging.SharedInstance.ApnsToken = result.RegistrationToken;

            var fcmToken = await InstanceId.SharedInstance.GetInstanceIdAsync();
            this.container.SetCurrentToken(fcmToken.Token, false);

            return new PushAccessState(result.Status, fcmToken.Token);
        }


        public async Task UnRegister()
        {
            this.container.ClearRegistration();
            await InstanceId.SharedInstance.DeleteIdAsync().ConfigureAwait(false);
            await this.adapter.UnRegister().ConfigureAwait(false);
        }


        public async Task AddTag(string tag)
        {
            var tags = this.RegisteredTags?.ToList() ?? new List<string>(1);
            tags.Add(tag);

            await Messaging.SharedInstance.SubscribeAsync(tag);
            this.container.RegisteredTags = tags.ToArray();
        }


        public async Task RemoveTag(string tag)
        {
            await Messaging.SharedInstance.UnsubscribeAsync(tag);
            if (this.RegisteredTags != null)
            {
                var tags = this.RegisteredTags.ToList();
                if (tags.Remove(tag))
                    this.container.RegisteredTags = tags.ToArray();
            }
        }


        public async Task ClearTags()
        {
            if (this.RegisteredTags != null)
                foreach (var tag in this.RegisteredTags)
                    await Messaging.SharedInstance.UnsubscribeAsync(tag);

            this.container.RegisteredTags = null;
        }


        public async Task SetTags(params string[]? tags)
        {
            await this.ClearTags();
            if (tags != null)
                foreach (var tag in tags)
                    await this.AddTag(tag);
        }
    }
}

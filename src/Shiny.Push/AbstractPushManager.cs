﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Stores;


namespace Shiny.Push
{
    public abstract class AbstractPushManager : IPushManager
    {
        protected AbstractPushManager(ShinyCoreServices services) => this.Services = services;


        protected ShinyCoreServices Services { get; }
        protected IKeyValueStore Settings => this.Services.Settings;
        public abstract IObservable<PushNotification> WhenReceived();
        public abstract Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default);
        public abstract Task UnRegister();


        protected void ClearRegistration()
        {
            this.CurrentRegistrationToken = null;
            this.CurrentRegistrationTokenDate = null;
            this.RegisteredTags = null;
        }


        public string? CurrentRegistrationToken
        {
            get => this.Settings.Get<string?>(nameof(this.CurrentRegistrationToken));
            protected set => this.Settings.SetOrRemove(nameof(this.CurrentRegistrationToken), value);
        }


        public DateTime? CurrentRegistrationTokenDate
        {
            get => this.Settings.Get<DateTime?>(nameof(this.CurrentRegistrationTokenDate));
            protected set => this.Settings.SetOrRemove(nameof(this.CurrentRegistrationTokenDate), value);
        }


        public string[]? RegisteredTags
        {
            get => this.Settings.Get<string[]?>(nameof(this.RegisteredTags));
            protected set => this.Settings.SetOrRemove(nameof(this.RegisteredTags), value);
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Settings;


namespace Shiny.Push
{
    public abstract class AbstractPushManager : IPushManager
    {
        protected AbstractPushManager(ISettings settings) => this.Settings = settings;
        protected ISettings Settings { get; }
        public abstract IObservable<IDictionary<string, string>> WhenReceived();
        public abstract Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default);
        public abstract Task UnRegister();


        protected virtual void ClearRegistration()
        {
            this.CurrentRegistrationExpiryDate = null;
            this.CurrentRegistrationToken = null;
            this.CurrentRegistrationTokenDate = null;
        }


        public string? CurrentRegistrationToken
        {
            get => this.Settings.Get<string?>(nameof(this.CurrentRegistrationToken));
            protected set => this.Settings.Set(nameof(this.CurrentRegistrationToken), value);
        }


        public DateTime? CurrentRegistrationTokenDate
        {
            get => this.Settings.Get<DateTime?>(nameof(this.CurrentRegistrationTokenDate));
            protected set => this.Settings.Set(nameof(this.CurrentRegistrationTokenDate), value);
        }


        public DateTime? CurrentRegistrationExpiryDate
        {
            get => this.Settings.Get<DateTime?>(nameof(this.CurrentRegistrationExpiryDate));
            protected set => this.Settings.Set(nameof(this.CurrentRegistrationExpiryDate), value);
        }


        public string[]? RegisteredTags
        {
            get => this.Settings.Get<string[]?>(nameof(this.RegisteredTags));
            protected set => this.Settings.Get(nameof(this.RegisteredTags), value);
        }
    }
}
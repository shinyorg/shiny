using System;
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
            this.CurrentRegistrationExpiryDate = null;
        }


        public string? CurrentRegistrationToken
        {
            get => this.Settings.Get<string?>(nameof(CurrentRegistrationToken));
            protected set => this.Settings.SetRegToken(value);
        }


        public DateTime? CurrentRegistrationTokenDate
        {
            get => this.Settings.Get<DateTime?>(nameof(CurrentRegistrationTokenDate));
            protected set => this.Settings.SetRegDate(value);
        }

        public DateTime? CurrentRegistrationExpiryDate
        {
            get => this.Settings.Get<DateTime?>(nameof(CurrentRegistrationExpiryDate));
            protected set => this.Settings.Set(nameof(CurrentRegistrationExpiryDate), value);
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Settings;


namespace Shiny.Push
{
    public abstract class AbstractPushManager : IPushManager
    {
        readonly ISettings settings;
        protected AbstractPushManager(ISettings settings) => this.settings = settings;


        public abstract IObservable<IPushNotification> WhenReceived();
        public abstract Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default);
        public abstract Task UnRegister();

                
        public string? CurrentRegistrationToken
        {
            get => this.settings.Get<string?>(nameof(CurrentRegistrationToken));
            protected set => this.settings.SetRegToken(value);
        }


        public DateTime? CurrentRegistrationTokenDate
        {
            get => this.settings.Get<DateTime?>(nameof(CurrentRegistrationTokenDate));
            protected set => this.settings.SetRegDate(value);
        }
    }
}

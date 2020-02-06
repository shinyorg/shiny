using System;
using System.Threading.Tasks;
using Shiny.Settings;


namespace Shiny.Push
{
    public abstract class AbstractPushManager : IPushManager
    {
        readonly ISettings settings;
        protected AbstractPushManager(ISettings settings) => this.settings = settings;


        public abstract Task<PushAccessState> RequestAccess();
        public abstract Task UnRegister();

        
        public string? CurrentRegistrationToken
        {
            get => this.settings.Get<string?>(nameof(CurrentRegistrationToken));
            protected set => this.settings.Set(nameof(CurrentRegistrationToken), value);
        }


        public DateTime? CurrentRegistrationTokenDate
        {
            get => this.settings.Get<DateTime?>(nameof(CurrentRegistrationTokenDate));
            protected set => this.settings.Set(nameof(CurrentRegistrationTokenDate), value);
        }
    }
}

#if !NETSTANDARD
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Stores;


namespace Shiny.Push
{
    public class PushContainer : NotifyPropertyChanged
    {
        readonly IEnumerable<IPushDelegate> delegates;


        public PushContainer(IKeyValueStoreFactory storeFactory, IEnumerable<IPushDelegate> delegates)
        {
            this.Store = storeFactory.DefaultStore;
            this.delegates = delegates;
        }


        public Task OnTokenRefreshed(string token)
            => this.delegates.RunDelegates(x => x.OnTokenRefreshed(token));

        public Task OnReceived(PushNotification push)
            => this.delegates.RunDelegates(x => x.OnReceived(push));

        public Task OnEntry(PushNotificationResponse response)
            => this.delegates.RunDelegates(x => x.OnEntry(response));


        public IKeyValueStore Store { get; }
        public void SetCurrentToken(string token)
        {
            this.CurrentRegistrationToken = token;
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;
        }


        public void ClearRegistration()
        {
            this.CurrentRegistrationToken = null;
            this.CurrentRegistrationTokenDate = null;
            this.RegisteredTags = null;
        }


        public string? CurrentRegistrationToken
        {
            get => this.Store.Get<string?>(nameof(this.CurrentRegistrationToken));
            set => this.Store.SetOrRemove(nameof(this.CurrentRegistrationToken), value);
        }


        public DateTime? CurrentRegistrationTokenDate
        {
            get => this.Store.Get<DateTime?>(nameof(this.CurrentRegistrationTokenDate));
            set => this.Store.SetOrRemove(nameof(this.CurrentRegistrationTokenDate), value);
        }


        public string[]? RegisteredTags
        {
            get => this.Store.Get<string[]?>(nameof(this.RegisteredTags));
            set => this.Store.SetOrRemove(nameof(this.RegisteredTags), value);
        }
    }
}
#endif
using System;
using Acr.UserDialogs.Forms;
using ReactiveUI.Fody.Helpers;
using Shiny.Net;


namespace $safeprojectname$
{
    public abstract class ServicesViewModel : ViewModel
    {
        protected ServicesViewModel(ICoreServices core)
        {
            this.Core = core;
            this.Core
                .Connectivity
                .WhenInternetStatusChanged()
                .ToPropertyEx(this, x => x.IsNetworkAvailable);
        }


        public ICoreServices Core { get; }
        protected IUserDialogs Dialogs => this.Core.Dialogs;
        public ILocalize Localize => this.Core.Localize;
        public string this[string key] => this.Localize[key];
        public bool IsNetworkAvailable { [ObservableAsProperty] get; }
    }
}

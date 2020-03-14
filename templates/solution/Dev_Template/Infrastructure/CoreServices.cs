using System;
using Acr.UserDialogs.Forms;
using Shiny;
using Shiny.Net;
using Shiny.Logging;


namespace $safeprojectname$.Infrastructure
{
    public class CoreServices
    {
        public CoreServices(ILocalize localize,
                            IAppSettings appSettings,
                            IUserDialogs dialogs,
                            IConnectivity connectivity)
        {
            this.Localize = localize;
            this.AppSettings = appSettings;            
            this.Dialogs = dialogs;
            this.Connectivity = connectivity;
        }


        public ILocalize Localize { get; }
        public IAppSettings AppSettings { get; }
        public IUserDialogs Dialogs { get; }
        public IConnectivity Connectivity { get; }
    }
}

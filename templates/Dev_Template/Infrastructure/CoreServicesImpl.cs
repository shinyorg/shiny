using System;
using Acr.UserDialogs.Forms;
using Shiny;
using Shiny.Net;
using Shiny.Logging;


namespace $safeprojectname$.Infrastructure
{
    public class CoreServicesImpl : ICoreServices
    {
        public CoreServicesImpl(ILocalize localize,
                                ILogger logger,
                                IUserDialogs dialogs,
                                IConnectivity connectivity)
        {
            this.Localize = localize;
            this.Logger = logger;
            this.Dialogs = dialogs;
            this.Connectivity = connectivity;
        }


        public ILocalize Localize { get; }
        public ILogger Logger { get; }
        public IUserDialogs Dialogs { get; }
        public IConnectivity Connectivity { get; }
    }
}

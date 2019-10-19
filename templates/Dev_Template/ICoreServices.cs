using System;
using Acr.UserDialogs.Forms;
using Shiny;
using Shiny.Net;
using Shiny.Logging;


namespace $safeprojectname$
{
    public interface ICoreServices
    {
        ILocalize Localize { get; }
        ILogger Logger { get; }
        IUserDialogs Dialogs { get; }
        IConnectivity Connectivity { get; }
    }
}

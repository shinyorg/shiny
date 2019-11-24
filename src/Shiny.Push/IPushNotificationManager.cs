using System;
using System.Threading.Tasks;


namespace Shiny.Push
{
    public interface IPushNotificationManager
    {
        //string CurrentRegistrationToken { get; }
        Task<PushAccessState> RequestAccess();

        // observable on notification access state?
        // old/new - event EventHandler<string> RegistrationTokenChanged;  - not really need - requestaccess should almost always be called
    }
}

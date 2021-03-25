using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Notifications;


namespace Shiny.Push
{
    public interface IPushDelegate
    {
        /// <summary>
        /// Called when the notification is triggered to the OS
        /// </summary>
        /// <param name="data">This is the data portion of the push notification</param>
        /// <param name="notification">The notification content - this can be null on iOS when received in the background or if a body was not sent</param>
        /// <param name="entry">if the user has tapped on an notification, this will true otherwise false</param>
        /// <returns></returns>
        Task OnAction(IDictionary<string, string> data, Notification? notification, bool entry);


        /// <summary>
        /// This is called ONLY when the token changes, not during RequestAccess
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task OnTokenChanged(string token);
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Notifications;


namespace Shiny.Push
{
    public interface IPushDelegate
    {
        /// <summary>
        /// This is called when the user taps/responds to a push notification
        /// </summary>
        /// <param name="data"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        Task OnEntry(IDictionary<string, string> data, NotificationResponse response);

        /// <summary>
        /// Called when a push is received. BACKGROUND NOTE: if your app is in the background, you need to pass data parameters (iOS: content-available:1) to get this to fire
        /// </summary>
        /// <param name="data">This is the data portion of the push notification</param>
        /// <param name="notification">The notification content - this can be null on iOS when received in the background or if a body was not sent</param>
        /// <returns></returns>
        Task OnReceived(IDictionary<string, string> data, Notification? notification);


        /// <summary>
        /// This is called ONLY when the token changes, not during RequestAccess
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task OnTokenChanged(string token);
    }
}

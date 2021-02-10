using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Push
{
    public interface IPushDelegate
    {
        /// <summary>
        /// This is fired when the user taps or responds to a notification
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        Task OnEntry(PushEntryArgs args);

        /// <summary>
        /// Called when the notification is triggered to the OS
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        Task OnReceived(IDictionary<string, string> data);

        /// <summary>
        /// This is called ONLY when the token changes, not during RequestAccess
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task OnTokenChanged(string token);
    }
}

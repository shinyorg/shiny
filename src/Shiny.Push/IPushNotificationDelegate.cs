using System;
using System.Threading.Tasks;


namespace Shiny.Push
{
    public interface IPushNotificationDelegate
    {
        Task OnReceived(string payload);
    }
}

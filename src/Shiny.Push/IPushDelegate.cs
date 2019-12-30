using System;
using System.Threading.Tasks;


namespace Shiny.Push
{
    public interface IPushDelegate
    {
        Task OnReceived(string payload);
    }
}

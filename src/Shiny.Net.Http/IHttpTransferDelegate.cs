using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;

namespace Shiny.Net.Http
{
    public interface IHttpTransferDelegate : IShinyDelegate
    {
        Task OnError(HttpTransfer transfer, Exception ex);
        Task OnCompleted(HttpTransfer transfer);
    }
}

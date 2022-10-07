using System;
using System.Threading.Tasks;

namespace Shiny.Net.Http;

public interface IHttpTransferDelegate
{
    Task OnError(HttpTransfer transfer, Exception ex);
    Task OnCompleted(HttpTransfer transfer);
}

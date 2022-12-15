using System;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


public interface IHttpTransferDelegate
{
    Task OnError(IHttpTransfer transfer, Exception ex);
    Task OnCompleted(IHttpTransfer transfer);
}
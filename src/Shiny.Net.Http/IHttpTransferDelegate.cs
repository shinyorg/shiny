using System;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


public interface IHttpTransferDelegate
{
    // could offer a chance to ask delegate if error should continue? maybe put that in a separate concern?
    Task OnError(HttpTransferRequest request, Exception ex);
    Task OnCompleted(HttpTransferRequest request);
}
using System;
using System.Threading.Tasks;
using Shiny.Net.Http;

public class HttpTransferDelegate : IHttpTransferDelegate
{
    public async Task OnCompleted(HttpTransfer transfer)
    {
    }

    public async Task OnError(HttpTransfer transfer, Exception ex)
    {
    }
}
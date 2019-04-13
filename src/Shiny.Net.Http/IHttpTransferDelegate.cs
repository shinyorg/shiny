using System;


namespace Shiny.Net.Http
{
    public interface IHttpTransferDelegate
    {
        void OnError(HttpTransfer transfer, Exception ex);
        void OnCompleted(HttpTransfer transfer);
    }
}

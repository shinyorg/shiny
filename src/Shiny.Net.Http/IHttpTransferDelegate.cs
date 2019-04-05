using System;


namespace Shiny.Net.Http
{
    public interface IHttpTransferDelegate
    {
        void OnError(IHttpTransfer transfer, Exception ex);
        void OnCompleted(IHttpTransfer transfer);
    }
}

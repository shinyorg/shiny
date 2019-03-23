using System;


namespace Shiny.Net.Http
{
    public interface IHttpTransferDelegate
    {
        void OnStatusChanged(IHttpTransfer transfer);
        void OnError(IHttpTransfer transfer, Exception ex);
        void OnCompleted(IHttpTransfer transfer);
        //void OnAuthRequired(IHttpTransfer transfer);
    }
}

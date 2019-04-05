using System;


namespace Shiny.Net.Http
{
    public interface IHttpTransfer
    {
        HttpTransferRequest Request { get; }
        string Identifier { get; }
        HttpTransferState Status { get; }
        Exception Exception { get; }
        string RemoteFileName { get; }
        long FileSize { get; }
        long BytesTransferred { get; }
        DateTime LastModified { get; }

        //HttpTransferMetrics CalculateMetrics();
    }
}
using System;

namespace Shiny.Net.Http;


public interface IHttpTransfer
{
    HttpTransferRequest Request { get; }

    HttpTransferState Status { get; }
    string Identifier { get; }
    long BytesTransferred { get; }
    long BytesToTransfer { get; }

    double PercentComplete { get; }
    IObservable<HttpTransferMetrics> ListenToMetrics();
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


public interface IHttpTransferManager
{
    Task<IList<HttpTransfer>> GetTransfers();
    Task<HttpTransfer> Queue(HttpTransferRequest request);
    Task Cancel(string identifier);
    Task CancelAll();

    IObservable<int> WatchCount();
    IObservable<HttpTransferResult> WhenUpdateReceived();
}

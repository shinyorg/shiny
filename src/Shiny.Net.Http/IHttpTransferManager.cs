using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


public interface IHttpTransferManager
{
    ValueTask<IList<HttpTransfer>> GetTransfers();
    ValueTask<HttpTransfer> Queue(HttpTransferRequest request);
    ValueTask Cancel(string identifier);
    ValueTask CancelAll();

    IObservable<HttpTransferResult> WhenUpdateReceived();
}

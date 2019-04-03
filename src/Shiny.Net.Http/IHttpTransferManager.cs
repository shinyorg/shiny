using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Net.Http
{
    public interface IHttpTransferManager
    {
        Task Cancel(IHttpTransfer transfer);
        Task<IEnumerable<IHttpTransfer>> GetTransfers();
        Task<IHttpTransfer> Enqueue(HttpTransferRequest request);
        Task CancelAll();
    }
}

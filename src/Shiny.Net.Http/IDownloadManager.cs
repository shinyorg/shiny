using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Net.Http
{
    public interface IDownloadManager
    {
        Task Cancel(IHttpTransfer transfer);
        Task<IEnumerable<IHttpTransfer>> GetTransfers();
        Task<IHttpTransfer> Create(HttpTransferRequest request);
        Task CancelAll();
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiny.Net.Http
{
    public interface IDownloadManager
    {
        Task<IEnumerable<IHttpTransfer>> GetTransfers();
        Task<IHttpTransfer> Create(HttpTransferRequest request);
        Task CancelAll();
    }
}

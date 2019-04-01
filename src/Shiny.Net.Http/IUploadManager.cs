using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Net.Http
{
    public interface IUploadManager
    {
        Task<IEnumerable<IHttpTransfer>> GetTransfers();
        Task<IHttpTransfer> Create(HttpTransferRequest request);
        Task Cancel(IHttpTransfer transfer);
        Task CancelAll();
    }
}

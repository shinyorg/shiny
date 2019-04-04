using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Net.Http
{
    public interface IHttpTransferManager
    {
        Task<IHttpTransfer> Enqueue(HttpTransferRequest request);
        Task<IEnumerable<IHttpTransfer>> GetTransfers(QueryFilter filter = null);
        Task Cancel(IHttpTransfer transfer = null);
        Task Cancel(QueryFilter filter);
    }
}

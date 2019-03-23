using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Net.Http
{
    public class DownloadManager : IDownloadManager
    {
        public Task CancelAll()
        {
            throw new NotImplementedException();
        }

        public Task<IHttpTransfer> Create(HttpTransferRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IHttpTransfer>> GetTransfers()
        {
            throw new NotImplementedException();
        }
    }
}

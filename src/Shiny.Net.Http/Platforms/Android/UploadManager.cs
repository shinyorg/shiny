using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;

namespace Shiny.Net.Http
{
    public class UploadManager : IUploadManager
    {
        readonly IAndroidContext context;
        readonly IRepository repository;


        public UploadManager(IAndroidContext context, IRepository repository)
        {
            this.context = context;
            this.repository = repository;
        }


        public Task Cancel(IHttpTransfer transfer)
        {
            throw new NotImplementedException();
        }


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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Net.Http
{
    public class UploadManager : IUploadManager
    {
        public Task CancelAll()
        {
            throw new NotImplementedException();
        }


        public Task<IHttpTransfer> Create(HttpTransferRequest request)
        {
            //BackgroundUploader
            //               .GetCurrentUploadsAsync()
            //               .AsTask()
            //               .ContinueWith(result =>
            //               {
            //                   foreach (var task in result.Result)
            //                   {
            //                       var config = new HttpTransferConfiguration(task.RequestedUri.ToString(), task.SourceFile.Path)
            //                       {
            //                           HttpMethod = task.Method,
            //                           UseMeteredConnection = task.CostPolicy != BackgroundTransferCostPolicy.UnrestrictedOnly
            //                       };
            //                       this.Add(new UploadHttpTransfer(config, task, true));
            //                   }
            //               });
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IHttpTransfer>> GetTransfers()
        {
            throw new NotImplementedException();
        }
    }
}

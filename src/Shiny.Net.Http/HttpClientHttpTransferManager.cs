using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;


namespace Shiny.Net.Http
{
    public class HttpClientHttpTransferManager : AbstractHttpTransferManager
    {
        readonly IJobManager jobManager;
        readonly IMessageBus messageBus;
        readonly IRepository repository;


        public HttpClientHttpTransferManager(IJobManager jobManager,
                                             IMessageBus messageBus,
                                             IRepository repository)
        {
            this.jobManager = jobManager;
            this.messageBus = messageBus;
            this.repository = repository;
        }


        // TODO: what if in the middle of job?
        public override Task Cancel(string id) => this.jobManager.Cancel(id);


        protected override async Task<HttpTransfer> CreateDownload(HttpTransferRequest request)
        {
            var id = Guid.NewGuid().ToString();
            await this.repository.Set(id, new HttpTransferStore
            {
                Id = id
            });
            await this.jobManager.Schedule(new JobInfo
            {
                Identifier = id,
                Type = typeof(TransferJob),
                RequiredInternetAccess = InternetAccess.Any,
                Repeat = true
            });
            var transfer = new HttpTransfer(
                id,
                request.Uri,
                request.LocalFile.FullName,
                true,
                request.UseMeteredConnection,
                null,
                request.LocalFile.Length,
                0,
                HttpTransferState.Pending
            );

            this.jobManager.Run(id);
            return transfer;
        }


        protected override async Task<HttpTransfer> CreateUpload(HttpTransferRequest request)
        {
            var id = Guid.NewGuid().ToString();
            await this.repository.Set(id, new HttpTransferStore
            {

            });
            var transfer = new HttpTransfer(
                id,
                request.Uri,
                request.LocalFile.FullName,
                true,
                request.UseMeteredConnection,
                null,
                request.LocalFile.Length,
                0,
                HttpTransferState.Pending
            );
            this.jobManager.Run(id);
            return transfer;

        }


        // overrides will have to merge with the base if they are only overriding one of the directions
        public override IObservable<HttpTransfer> WhenUpdated()
            => this.messageBus.Listener<HttpTransfer>();
    }
}
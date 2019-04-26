using System;
using System.Collections.Generic;
using System.Linq;
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


        protected override Task<IEnumerable<HttpTransfer>> GetDownloads(QueryFilter filter)
            => this.Query(filter, false);


        protected override Task<IEnumerable<HttpTransfer>> GetUploads(QueryFilter filter)
            => this.Query(filter, true);


        async Task<IEnumerable<HttpTransfer>> Query(QueryFilter filter, bool isUpload)
        {
            var stores = await this.repository
                .GetAll<HttpTransferStore>()
                .ConfigureAwait(false);

            var query = stores
                .Where(x => x.IsUpload == isUpload);

            if (filter.Ids.Any())
                query = query.Where(x => filter.Ids.Any(y => x.Id == y));

            // TODO: get attributes (filesize, bytes xfer, etc)
            return query.Select(x => new HttpTransfer(
                x.Id,
                x.Uri,
                x.LocalFile,
                isUpload,
                x.UseMeteredConnection,
                null,
                0L,
                0L,
                HttpTransferState.Pending
            ));
        }

        // TODO: what if in the middle of job?
        public override Task Cancel(string id) => this.jobManager.Cancel(id);


        protected override Task<HttpTransfer> CreateDownload(HttpTransferRequest request)
            => this.Create(request);


        protected override Task<HttpTransfer> CreateUpload(HttpTransferRequest request)
            => this.Create(request);


        async Task<HttpTransfer> Create(HttpTransferRequest request)
        {
            var id = Guid.NewGuid().ToString();
            await this.repository.Set(id, new HttpTransferStore
            {
                Id = id,
                Uri = request.Uri,
                IsUpload = request.IsUpload,
                PostData = request.PostData,
                LocalFile = request.LocalFile.FullName,
                UseMeteredConnection = request.UseMeteredConnection,
                HttpMethod = request.HttpMethod.ToString(),
                Headers = request.Headers
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
                request.IsUpload,
                request.UseMeteredConnection,
                null,
                request.IsUpload ? request.LocalFile.Length : 0L,
                0,
                HttpTransferState.Pending
            );
            // fire and forget
            this.jobManager.Run(id);
            return transfer;
        }


        // overrides will have to merge with the base if they are only overriding one of the directions
        public override IObservable<HttpTransfer> WhenUpdated()
            => this.messageBus.Listener<HttpTransfer>();
    }
}
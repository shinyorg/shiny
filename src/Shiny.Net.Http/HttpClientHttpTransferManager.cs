using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Infrastructure;
using Shiny.Jobs;
using Shiny.Net.Http.Infrastructure;


namespace Shiny.Net.Http
{
    public class HttpClientHttpTransferManager : AbstractHttpTransferManager, IShinyStartupTask
    {
        readonly ILogger logger;


        public HttpClientHttpTransferManager(ShinyCoreServices services, ILogger logger)
        {
            this.Services = services;
            this.logger = logger;
        }


        protected ShinyCoreServices Services { get; }

        protected override Task<IEnumerable<HttpTransfer>> GetDownloads(QueryFilter filter)
            => this.Query(filter, false);


        protected override Task<IEnumerable<HttpTransfer>> GetUploads(QueryFilter filter)
            => this.Query(filter, true);


        async Task<IEnumerable<HttpTransfer>> Query(QueryFilter filter, bool isUpload)
        {
            var stores = await this.Services
                .Repository
                .GetAll<HttpTransferStore>()
                .ConfigureAwait(false);

            var query = stores
                .Where(x => x.IsUpload == isUpload);

            if (filter.Ids.Any())
                query = query.Where(x => filter.Ids.Any(y => x.Id == y));

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


        public override Task Cancel(string id) => Task.WhenAll(
            this.Services.Jobs.Cancel(id),
            this.Services.Repository.Remove<HttpTransferStore>(id)
        );


        protected override Task<HttpTransfer> CreateDownload(HttpTransferRequest request)
            => this.Create(request);


        protected override Task<HttpTransfer> CreateUpload(HttpTransferRequest request)
            => this.Create(request);


        async Task<HttpTransfer> Create(HttpTransferRequest request)
        {
            var id = Guid.NewGuid().ToString();
            await this.Services.Repository.Set(id, new HttpTransferStore
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
            await this.Services.Jobs.Register(new JobInfo(typeof(TransferJob), id)
            {
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
            this.Services.Jobs.Run(id);
            return transfer;
        }


        // overrides will have to merge with the base if they are only overriding one of the directions
        public override IObservable<HttpTransfer> WhenUpdated()
            => this.Services.Bus.Listener<HttpTransfer>();


        public async void Start()
        {
            try
            {
                var requests = await this.Services.Repository.GetAll<HttpTransferStore>();
                foreach (var request in requests)
                    await this.Services.Jobs.Run(request.Id);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error restarting HTTP transfer manager");
            }
        }
    }
}
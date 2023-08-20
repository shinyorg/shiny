using System;
using System.Linq;
using System.Reactive.Linq;
using AndroidX.Core.App;
using Microsoft.Extensions.Logging;
using Shiny.Support.Repositories;

namespace Shiny.Net.Http;


public abstract class AndroidSummaryTransferNotification : IShinyStartupTask
{
    readonly AndroidPlatform platform;
    readonly IHttpTransferManager manager;
    readonly IRepository repository;
    readonly ILogger logger;
    readonly NotificationManagerCompat notifications;


    public AndroidSummaryTransferNotification(
        AndroidPlatform platform,
        IRepository repository,
        ILogger<AndroidSummaryTransferNotification> logger
    )
    {
        this.platform = platform;
        this.repository = repository;
        this.logger = logger;

        this.notifications = NotificationManagerCompat.From(this.platform.AppContext);
    }


    public void Start()
    {
        var transfers = this.repository.GetList<HttpTransfer>();
        var count = transfers.Count;
        var bytesToXfer = transfers.Sum(x => x.BytesToTransfer ?? 0);
        var bytesXfer = transfers.Sum(x => x.BytesTransferred);

        if (count > 0)
        {
            // TODO: set initial transfer
        }

        this.repository
            .WhenActionOccurs()
            //.TakeLastBuffer(TimeSpan.FromSeconds(2)) // TODO: can only do this if update
            .Where(x => x.EntityType == typeof(HttpTransfer))
            .Subscribe(x =>
            {
                if (x.Action == RepositoryAction.Clear)
                {
                    // TODO: remove notification
                    this.notifications.Cancel(0);
                }
                else
                {
                    // TODO: what about indeterministic transfers?
                    var xfer = (HttpTransfer)x.Entity!;

                    switch (x.Action)
                    {
                        case RepositoryAction.Add:
                            count++;
                            bytesToXfer += xfer.BytesToTransfer ?? 0;
                            break;

                        case RepositoryAction.Update:
                            bytesXfer += xfer.BytesTransferred;
                            break;

                        case RepositoryAction.Remove:
                            count--;

                            if (count == 0)
                            {
                                this.notifications.Cancel(0);
                            }
                            else if (xfer.Status != HttpTransferState.Completed)
                            {
                                bytesToXfer -= xfer.BytesToTransfer ?? 0;
                                bytesXfer -= xfer.BytesTransferred;
                            }
                            break;
                    }
                    if (count > 0)
                    {
                        
                    }
                }
            });
    }


    // TODO: time estimate based on bps (does it apply for different sites though?)
    // TODO: bps
    // TODO: percentage
    protected abstract void Customize(
        NotificationCompat.Builder builder,
        int transferCount,
        long bytesTransferred,
        long bytesToTransfer
    );
}
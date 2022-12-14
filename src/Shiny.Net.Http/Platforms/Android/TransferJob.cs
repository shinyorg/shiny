using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;
using Shiny.Stores;
using Shiny.Stores.Impl;

namespace Shiny.Net.Http;


class TransferJob : IJob
{
    readonly IConnectivity connectivity;
    readonly IHttpTransferManager manager;
    readonly IEnumerable<IHttpTransferDelegate> delegates;
    

    public TransferJob(
        IHttpTransferManager manager,
        IConnectivity connectivity,
        IEnumerable<IHttpTransferDelegate> delegates
    )
    {
        this.manager = manager;
        this.connectivity = connectivity;        
        this.delegates = delegates;
    }


    public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
    {
        // this job is purely in android, so we have access to all android stuff        
        var expensiveNetwork = connectivity.Access.HasFlag(NetworkAccess.ConstrainedInternet);

        foreach (var transfer in this.manager.Transfers)
        {
            if (transfer.Request.UseMeteredConnection || !expensiveNetwork)
            {
                // TODO: allow configurable amount of transfer at a time
                await this.manager.Resume(transfer.Identifier);

                // TODO: start persistent notification

                // wait for pause/complete/error/cancel then move to next transfer
                await transfer
                    .ListenToMetrics()
                    .Do(x =>
                    {
                        if (x.Status == HttpTransferState.InProgress)
                        {
                            // TODO: update persistent notification
                        }
                        else
                        {
                            // TODO: cancel persistent notification
                        }
                    })
                    .Where(x =>
                        x.Status != HttpTransferState.InProgress &&
                        x.Status != HttpTransferState.Pending
                    )
                    .Take(1)
                    .ToTask(cancelToken)
                    .ConfigureAwait(false);
            }
        }
    }


    void StartNotification(IHttpTransfer transfer)
    {

    }
}

// TODO: persistent notifications with progress
//native.SetAllowedNetworkTypes(DownloadNetwork.Wifi)
//native.SetNotificationVisibility(DownloadVisibility.Visible);
//native.SetRequiresDeviceIdle
//native.SetRequiresCharging
//native.SetTitle("")
//native.SetDescription()
//native.SetVisibleInDownloadsUi(true);
//native.SetShowRunningNotification
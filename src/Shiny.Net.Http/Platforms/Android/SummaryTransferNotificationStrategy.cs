//using System;
//using System.Linq;
//using System.Reactive.Linq;
//using AndroidX.Core.App;
//using Microsoft.Extensions.Logging;
//using Shiny.Support.Repositories;

//namespace Shiny.Net.Http;


//public class SummaryTransferNotificationStrategy : AbstractTransferNotificationStrategy
//{

//    public SummaryTransferNotificationStrategy(
//        IRepository repository,
//        AndroidPlatform platform,
//        ILogger<SummaryTransferNotificationStrategy> logger
//    ) : base(platform, logger)
//    {
//        this.Repository = repository;
//    }

//    protected IRepository Repository { get; }


//    public override void Start()
//    {
//        // TODO: this is only good for uploads really
//        var notificationId = new Random().Next(25000, 999999);
//        var channelId = this.CreateChannel();
//        var builder = this.CreateBuilder(channelId);

//        var transfers = this.Repository.GetList<HttpTransfer>();
//        var count = transfers.Count;
//        var bytesToXfer = transfers.Sum(x => x.BytesToTransfer ?? 0);
//        var bytesXfer = transfers.Sum(x => x.BytesTransferred);

//        if (count > 0)
//        {
//            // TODO: set initial transfer
//            //this.Customize(builder)
//            this.NotificationManager.Notify(notificationId, builder.Build());
//        }

//        this.Repository
//            .WhenActionOccurs()
//            //.TakeLastBuffer(TimeSpan.FromSeconds(2)) // TODO: can only do this if update
//            .Where(x => x.EntityType == typeof(HttpTransfer))
//            .Subscribe(x =>
//            {
//                if (x.Action == RepositoryAction.Clear)
//                {
//                    // TODO: remove notification
//                    this.NotificationManager.Cancel(notificationId);
//                }
//                else
//                {
//                    // TODO: what about indeterministic transfers?
//                    var xfer = (HttpTransfer)x.Entity!;

//                    switch (x.Action)
//                    {
//                        case RepositoryAction.Add:
//                            count++;
//                            bytesToXfer += xfer.BytesToTransfer ?? 0;
//                            break;

//                        case RepositoryAction.Update:
//                            bytesXfer += xfer.BytesTransferred;
//                            break;

//                        case RepositoryAction.Remove:
//                            count--;

//                            if (count == 0)
//                            {
//                                this.NotificationManager.Cancel(0);
//                            }
//                            else if (xfer.Status != HttpTransferState.Completed)
//                            {
//                                bytesToXfer -= xfer.BytesToTransfer ?? 0;
//                                bytesXfer -= xfer.BytesTransferred;
//                            }
//                            break;
//                    }
//                    if (count > 0)
//                    {
//                        //this.Customize(builder,
//                        this.NotificationManager.Notify(notificationId, builder.Build());
//                    }
//                }
//            });
//    }


//    protected virtual void Customize(
//        NotificationCompat.Builder builder,
//        int transferRemaining,
//        int transferCount,
//        TimeSpan EstimatedTimeRemaining,
//        double percentage,
//        long bytesToTransfer,
//        long bytesTransferred,
//        long bytesPerSecond
//    )
//    {

//    }
//}
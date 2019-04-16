using System;
using Windows.Networking.BackgroundTransfer;


namespace Shiny.Net.Http
{
    public static class Extensions
    {
        public static HttpTransfer FromNative(this DownloadOperation x) => new HttpTransfer(
            x.Guid.ToString(),
            x.RequestedUri.ToString(),
            x.ResultFile.Path,
            false,
            x.CostPolicy == BackgroundTransferCostPolicy.Always,
            null,
            (long)x.Progress.TotalBytesToReceive,
            (long)x.Progress.BytesReceived,
            x.Progress.Status.FromNative()
        );


        public static HttpTransfer FromNative(this UploadOperation x) => new HttpTransfer(
            x.Guid.ToString(),
            x.RequestedUri.ToString(),
            x.SourceFile.Path,
            true,
            x.CostPolicy == BackgroundTransferCostPolicy.Always,
            null,
            (long)x.Progress.TotalBytesToSend,
            (long)x.Progress.BytesSent,
            x.Progress.Status.FromNative()
        );


        public static HttpTransferState FromNative(this BackgroundTransferStatus status)
        {
            switch (status)
            {
                case BackgroundTransferStatus.Canceled:
                    return HttpTransferState.Cancelled;

                case BackgroundTransferStatus.Completed:
                    return HttpTransferState.Completed;

                case BackgroundTransferStatus.Error:
                    return HttpTransferState.Error;

                case BackgroundTransferStatus.Idle:
                    return HttpTransferState.Pending;

                case BackgroundTransferStatus.PausedByApplication:
                    return HttpTransferState.Paused;

                case BackgroundTransferStatus.PausedSystemPolicy:
                case BackgroundTransferStatus.PausedCostedNetwork:
                    return HttpTransferState.PausedByCostedNetwork;

                case BackgroundTransferStatus.PausedNoNetwork:
                    return HttpTransferState.PausedByNoNetwork;

                case BackgroundTransferStatus.PausedRecoverableWebErrorStatus:
                    return HttpTransferState.Retrying;

                case BackgroundTransferStatus.Running:
                    return HttpTransferState.InProgress;

                default:
                    return HttpTransferState.Unknown;
            }
        }
    }
}

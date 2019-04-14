using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundation;


namespace Shiny.Net.Http
{
    static class Extensions
    {
        public static NSUrlRequest ToNative(this HttpTransferRequest request)
        {
            var url = NSUrl.FromString(request.Uri);
            var native = new NSMutableUrlRequest(url)
            {
                HttpMethod = request.HttpMethod.Method,
                AllowsCellularAccess = request.UseMeteredConnection,
            };
            if (!request.PostData.IsEmpty())
                native.Body = NSData.FromString(request.PostData);

            if (request.Headers.Any())
                native.Headers = NSDictionary.FromObjectsAndKeys(
                    request.Headers.Values.ToArray(),
                    request.Headers.Keys.ToArray()
                );

            return native;
        }


        public static async Task<IEnumerable<HttpTransfer>> QueryTransfers(this NSUrlSession session, QueryFilter filter)
        {
            var tasks = await session.QueryTasks(filter);
            return tasks.Select(x => x.FromNative());
        }


        public static async Task<IEnumerable<NSUrlSessionTask>> QueryTasks(this NSUrlSession session, QueryFilter filter)
        {
            var tasks = await session.GetAllTasksAsync();
            var results = tasks.AsEnumerable();

            if (filter != null)
            {
                switch (filter.Direction)
                {
                    case DirectionFilter.Upload:
                        results = results.Where(x => x is NSUrlSessionUploadTask);
                        break;

                    case DirectionFilter.Download:
                        results = results.Where(x => x is NSUrlSessionDownloadTask);
                        break;
                }
                if (filter.Ids.Any())
                {
                    var ids = filter.Ids.Select(nuint.Parse);
                    results = results.Where(x => ids.Any(y => y == x.TaskIdentifier));
                }
                switch (filter.States)
                {
                    case HttpTransferStateFilter.InProgress:
                        results = results.Where(x => x.GetTransferStatus() == HttpTransferState.InProgress);
                        break;

                    case HttpTransferStateFilter.Pending:
                        results = results.Where(x => x.GetTransferStatus() == HttpTransferState.Pending);
                        break;

                        // TODO: paused
                }
            }
            return results;
        }


        public static HttpTransfer FromNative(this NSUrlSessionTask task)
        {
            var upload = task is NSUrlSessionUploadTask;
            Exception exception = null;
            if (task.Error != null)
                exception = new Exception(task.Error.LocalizedDescription);

            return new HttpTransfer(
                task.TaskIdentifier.ToString(),
                task.OriginalRequest.Url.ToString(),
                task.TaskDescription,
                upload,
                task.OriginalRequest.AllowsCellularAccess,
                exception,
                upload
                    ? task.TaskDescription
                    : "", // TODO: remote filename for download
                upload
                    ? task.BytesExpectedToSend
                    : task.BytesExpectedToReceive,
                upload
                    ? task.BytesSent
                    : task.BytesReceived,

                task.GetTransferStatus()
            );
        }


        public static HttpTransferState GetTransferStatus(this NSUrlSessionTask task)
        {
            NSUrlSessionTaskState? state = null;
            long bytes = 0;

            if (task is NSUrlSessionUploadTask up)
            {
                bytes = up.BytesSent;
                state = up.State;
            }
            else if (task is NSUrlSessionDownloadTask down)
            {
                bytes = down.BytesReceived;
                state = down.State;
            }
            if (state == null)
                return HttpTransferState.Unknown;

            switch (state)
            {
                case NSUrlSessionTaskState.Canceling:
                    return HttpTransferState.Cancelled;

                case NSUrlSessionTaskState.Running:
                    return bytes == 0 ? HttpTransferState.Pending : HttpTransferState.InProgress;

                case NSUrlSessionTaskState.Suspended:
                    return HttpTransferState.Paused;

                case NSUrlSessionTaskState.Completed:
                    return HttpTransferState.Completed;

                default:
                    return HttpTransferState.Unknown;
            }
        }
    }
}

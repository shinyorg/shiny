using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;


namespace Shiny.Net.Http
{
    static class PlatformExtensions
    {
        public static NSMutableUrlRequest ToNative(this HttpTransferRequest request)
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

        public static string CreateRequestBodyFile(this NSMutableUrlRequest native, HttpTransferRequest request)
        {
            var boundary = Guid.NewGuid().ToString();
            var bodyPath = Path.Combine(ShinyHost.Resolve<IPlatform>().AppData.FullName, $"{Guid.NewGuid()}_bodydata.tmp"); // This file needs to be removed after request has completed

            native["Content-Type"] = $"multipart/form-data; boundary={boundary}";
            native["FileName"] = request.LocalFile.Name;

            // Write body part to file
            using (var writeStream = new FileStream(bodyPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                var sb = new StringBuilder();

                if (!request.PostData.IsEmpty())
                {
                    sb.AppendLine($"--{boundary}");
                    sb.AppendLine("Content-Type: text/plain; charset=utf-8");
                    sb.AppendLine("Content-Disposition: form-data;");
                    sb.AppendLine();
                    sb.AppendLine(request.PostData);

                    writeStream.Write(Encoding.Default.GetBytes(sb.ToString()), 0, sb.Length);
                }

                using var fileStream = request.LocalFile.OpenRead();
                var filedata = fileStream.ToByteArray();

                if (filedata != null)
                {
                    sb.Clear();
                    sb.AppendLine($"--{boundary}");
                    sb.AppendLine($"Content-Disposition: form-data; name=\"blob\"; filename=\"{request.LocalFile.Name}\"");
                    sb.AppendLine("Content-Type: application/octet-stream");
                    sb.AppendLine();

                    writeStream.Write(Encoding.Default.GetBytes(sb.ToString()), 0, sb.Length);
                    writeStream.Write(filedata, 0, filedata.Length);

                    sb.Clear();
                    sb.AppendLine();
                    sb.AppendLine($"--{boundary}--");
                    writeStream.Write(Encoding.Default.GetBytes(sb.ToString()), 0, sb.Length);
                }
            }

            return bodyPath;
        }


        public static async Task<IEnumerable<HttpTransfer>> QueryTransfers(this NSUrlSession session, QueryFilter? filter)
        {
            var tasks = await session.QueryTasks(filter);
            return tasks.Select(x => x.FromNative());
        }


        public static async Task<IEnumerable<NSUrlSessionTask>> QueryTasks(this NSUrlSession session, QueryFilter? filter)
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
                    results = results.Where(x => filter.Ids.Any(y => x.TaskDescription.StartsWith(y)));

                //switch (filter.None)
                //{
                //    case HttpTransferStateFilter.InProgress:
                //        results = results.Where(x => x.GetTransferStatus() == HttpTransferState.InProgress);
                //        break;

                //    case HttpTransferStateFilter.Pending:
                //        results = results.Where(x => x.GetTransferStatus() == HttpTransferState.Pending);
                //        break;

                //        // paused?
                //}
            }
            return results;
        }


        public static HttpTransfer FromNative(this NSUrlSessionTask task)
        {
            var upload = task is NSUrlSessionUploadTask;
            Exception? exception = null;
            if (task.Error != null && task.State != NSUrlSessionTaskState.Canceling)
                exception = new Exception(task.Error.LocalizedDescription);

            var taskId = TaskIdentifier.FromString(task.TaskDescription);
            return new HttpTransfer(
                taskId.Value,
                task.OriginalRequest.Url.ToString(),
                taskId.File.FullName,
                upload,
                task.OriginalRequest.AllowsCellularAccess,
                exception,
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
            var bytes = task.BytesSent + task.BytesReceived;
            switch (task.State)
            {
                case NSUrlSessionTaskState.Canceling:
                    return HttpTransferState.Canceled;

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

        public static byte[]? ToByteArray(this Stream stream)
        {
            if (stream == null)
                return null;

            stream.Position = 0;

            if (stream is MemoryStream memoryStream)
            {
                return memoryStream.ToArray();
            }

            using (var memoryStream2 = new MemoryStream())
            {
                stream.CopyTo(memoryStream2);
                return memoryStream2.ToArray();
            }
        }
    }
}

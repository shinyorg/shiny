using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Foundation;

namespace Shiny.Net.Http;


static class PlatformExtensions
{
    public static void WriteString(this Stream stream, string value, bool includeNewLine = true)
    {
        stream.Write(Encoding.Default.GetBytes(value));
        if (includeNewLine)
            stream.WriteLine();
    }


    public static void WriteLine(this Stream stream)
        => stream.Write(Encoding.Default.GetBytes("\r\n"));


    public static NSMutableUrlRequest ToNative(this HttpTransferRequest request)
    {
        var url = NSUrl.FromString(request.Uri)!;
        var native = new NSMutableUrlRequest(url)
        {
            HttpMethod = request.HttpMethod?.Method ?? HttpMethod.Get.Method,
            AllowsExpensiveNetworkAccess = request.UseMeteredConnection
        };

        if (request.Headers?.Any() ?? false)
        {
            native.Headers = NSDictionary.FromObjectsAndKeys(
                request.Headers.Values.ToArray(),
                request.Headers.Keys.ToArray()
            );
        }
        return native;
    }


    public static HttpTransferState GetStatus(this NSUrlSessionTask task) => task.State switch
    {
        NSUrlSessionTaskState.Canceling => HttpTransferState.Canceled,
        NSUrlSessionTaskState.Completed => HttpTransferState.Completed,
        NSUrlSessionTaskState.Running => task.BytesSent > 0 || task.BytesReceived > 0
            ? HttpTransferState.InProgress
            : HttpTransferState.Pending,

        NSUrlSessionTaskState.Suspended => HttpTransferState.Paused,
        _ => HttpTransferState.Unknown
    };


    public static HttpTransfer FromNative(this NSUrlSessionTask task)
    {
        var request = new HttpTransferRequest(
            task.OriginalRequest!.Url.ToString(),
            task is NSUrlSessionUploadTask,
            new FileInfo(task.TaskDescription!),
            task.OriginalRequest.AllowsExpensiveNetworkAccess,
            task.OriginalRequest.Body?.ToString(),
            new HttpMethod(task.OriginalRequest.HttpMethod),
            task.OriginalRequest.Headers?.FromNsDictionary()
        );
        return new HttpTransfer(
            request,
            task
        );
    }


    public static string GetUploadTempFilePath(this IPlatform platform, HttpTransferRequest request)
        => GetUploadTempFilePath(platform, request.LocalFile.Name);


    static string GetUploadTempFilePath(IPlatform platform, string fileName)
    {
        var tempPath = Path.Combine(platform.Cache.FullName, fileName + ".tmp");
        return tempPath;
    }
}
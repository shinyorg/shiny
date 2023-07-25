using System;
using System.IO;
using System.Linq;
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


    public static bool HasError(this NSUrlSessionTask task)
    {
        if (task.Error != null)
            return true;

        var statusCode = task.GetStatusCode();
        return statusCode < 200 || statusCode > 299;
    }


    public static bool IsCancelled(this NSUrlSessionTask task)
    {
        if (task.State == NSUrlSessionTaskState.Canceling)
            return true;

        return (task.Error?.Code ?? 0) == -999;
    }


    public static int GetStatusCode(this NSUrlSessionTask task)
        => (int)((task.Response as NSHttpUrlResponse)?.StatusCode ?? 0);


    public static NSMutableUrlRequest ToNative(this HttpTransferRequest request)
    {
        var url = NSUrl.FromString(request.Uri)!;
        var native = new NSMutableUrlRequest(url)
        {
            HttpMethod = request.GetHttpMethod().Method,
            AllowsExpensiveNetworkAccess = request.UseMeteredConnection
        };

        if (request is AppleHttpTransferRequest appleRequest)
        {
            native.AllowsCellularAccess = appleRequest.AllowsCellularAccess;
            native.AllowsConstrainedNetworkAccess = appleRequest.AllowsConstrainedNetworkAccess;
            if (OperatingSystemShim.IsAppleVersionAtleast(14, 5) && appleRequest.AssumesHttp3Capable != null)
                native.AssumesHttp3Capable = appleRequest.AssumesHttp3Capable.Value;
        }

        if (!request.IsUpload && request.HttpContent != null)
        {
            native.Body = NSData.FromString(request.HttpContent.Content); //, NSStringEncoding.UTF8);
        }

        if (request.Headers?.Any() ?? false)
        {
            native.Headers = NSDictionary.FromObjectsAndKeys(
                request.Headers.Values.ToArray(),
                request.Headers.Keys.ToArray()
            );
        }
        return native;
    }


    //public static HttpTransferState GetStatus(this NSUrlSessionTask task) => task.State switch
    //{
    //    NSUrlSessionTaskState.Canceling => HttpTransferState.Canceled,
    //    NSUrlSessionTaskState.Completed => HttpTransferState.Completed,
    //    NSUrlSessionTaskState.Running => task.BytesSent > 0 || task.BytesReceived > 0
    //        ? HttpTransferState.InProgress
    //        : HttpTransferState.Pending,

    //    NSUrlSessionTaskState.Suspended => HttpTransferState.Paused,
    //    _ => HttpTransferState.Unknown
    //};


    public static string GetUploadTempFilePath(this IPlatform platform, HttpTransferRequest request)
        => GetUploadTempFilePath(platform, request.LocalFilePath);


    static string GetUploadTempFilePath(IPlatform platform, string fileName)
    {
        var tempPath = Path.Combine(platform.Cache.FullName, fileName + ".tmp");
        return tempPath;
    }
}
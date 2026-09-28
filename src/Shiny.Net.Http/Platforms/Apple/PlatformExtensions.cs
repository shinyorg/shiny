using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Foundation;

namespace Shiny.Net.Http;


static class PlatformExtensions
{
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


    public static NSMutableUrlRequest ToNative(
        this HttpTransferRequest request,
        string? uri = null,
        string? httpMethod = null,
        IDictionary<string, string>? extraHeaders = null
    )
    {
        var url = NSUrl.FromString(uri ?? request.Uri)!;
        var native = new NSMutableUrlRequest(url)
        {
            HttpMethod = httpMethod ?? request.GetHttpMethod().Method,
            AllowsExpensiveNetworkAccess = request.UseMeteredConnection
        };

        if (request is AppleHttpTransferRequest appleRequest)
        {
            native.AllowsCellularAccess = appleRequest.AllowsCellularAccess;
            native.AllowsConstrainedNetworkAccess = appleRequest.AllowsConstrainedNetworkAccess;
            if ((OperatingSystem.IsIOSVersionAtLeast(14, 5) || OperatingSystem.IsMacCatalystVersionAtLeast(14, 5)) && appleRequest.AssumesHttp3Capable != null)
                native.AssumesHttp3Capable = appleRequest.AssumesHttp3Capable.Value;
        }

        if (!request.Type.IsUpload() && request.HttpContent != null)
        {
            native.Body = NSData.FromString(request.HttpContent.Content); //, NSStringEncoding.UTF8);
        }

        var headers = new Dictionary<string, string>();
        if (request.Headers != null)
        {
            foreach (var header in request.Headers)
                headers[header.Key] = header.Value;
        }
        if (extraHeaders != null)
        {
            foreach (var header in extraHeaders)
                headers[header.Key] = header.Value;
        }

        if (headers.Count > 0)
        {
            native.Headers = NSDictionary.FromObjectsAndKeys(
                headers.Values.ToArray(),
                headers.Keys.ToArray()
            );
        }
        return native;
    }


    /// <summary>
    /// Reads a header from a task's response (case-insensitive, as HTTP headers are).
    /// </summary>
    public static string? GetResponseHeader(this NSUrlSessionTask task, string name)
    {
        var fields = (task.Response as NSHttpUrlResponse)?.AllHeaderFields;
        if (fields == null)
            return null;

        foreach (var key in fields.Keys)
        {
            if (String.Equals(key.ToString(), name, StringComparison.OrdinalIgnoreCase))
                return fields[key]?.ToString();
        }
        return null;
    }


    /// <summary>
    /// Reads a header this task was sent with.
    /// </summary>
    public static string? GetRequestHeader(this NSUrlSessionTask task, string name)
    {
        var headers = task.OriginalRequest?.Headers;
        if (headers == null)
            return null;

        foreach (var key in headers.Keys)
        {
            if (String.Equals(key.ToString(), name, StringComparison.OrdinalIgnoreCase))
                return headers[key]?.ToString();
        }
        return null;
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
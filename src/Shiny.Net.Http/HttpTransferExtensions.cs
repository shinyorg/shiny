using System;
using System.IO;
using System.Reactive.Linq;

namespace Shiny.Net.Http;


public static class HttpTransferExtensions
{
    //static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
    //public static string BytesToSizeSuffix(long value, int decimalPlaces = 1)
    //{
    //    if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
    //    if (value < 0)
    //        return "-" + BytesToSizeSuffix(-value, decimalPlaces);

    //    if (value == 0)
    //        return String.Format("{0:n" + decimalPlaces + "} bytes", 0); }

    //    // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
    //    int mag = (int)Math.Log(value, 1024);

    //    // 1L << (mag * 10) == 2 ^ (10 * mag) 
    //    // [i.e. the number of bytes in the unit corresponding to mag]
    //    decimal adjustedSize = (decimal)value / (1L << (mag * 10));

    //    // make adjustment when the value is large enough that
    //    // it would round up to 1000 or more
    //    if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
    //    {
    //        mag += 1;
    //        adjustedSize /= 1024;
    //    }

    //    return string.Format("{0:n" + decimalPlaces + "} {1}",
    //        adjustedSize,
    //        SizeSuffixes[mag]);
    //}


    /// <summary>
    /// Asserts that an HttpTransferRequest is valid
    /// </summary>
    /// <param name="request"></param>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static void AssertValid(this HttpTransferRequest request)
    {
        if (request.Identifier.IsEmpty())
            throw new InvalidOperationException("Identifier is not set");

        if (request.IsUpload)
        {
            if (!File.Exists(request.LocalFilePath))
                throw new ArgumentException($"{request.LocalFilePath} does not exist");
        }
    }


    /// <summary>
    /// Allows you to monitor a specific transfer - this will complete or error unlike the full WhenUpdateReceived stream
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="identifier"></param>
    /// <returns></returns>
    public static IObservable<HttpTransferResult> WatchTransfer(this IHttpTransferManager manager, string identifier)
        => Observable.Create<HttpTransferResult>(ob =>
            manager
                .WhenUpdateReceived()
                .Where(x =>
                {
                    return x.Request.Identifier.Equals(identifier, StringComparison.InvariantCultureIgnoreCase);
                })
                .Subscribe(
                    x =>
                    {
                        if (x.Exception != null)
                        {
                            ob.OnError(x.Exception);
                        }
                        else if (x.Status == HttpTransferState.Completed)
                        {
                            ob.OnNext(x);
                            ob.OnCompleted();
                        }
                        else
                        {
                            ob.OnNext(x);
                        }
                    },
                    ob.OnError
                )
        );
}
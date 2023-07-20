using System;
using System.IO;
using System.Reactive.Linq;

namespace Shiny.Net.Http;


public static class HttpTransferExtensions
{
    /// <summary>
    /// Tries to calculate HTTP transfer percentage complete - returns null if insufficient information
    /// </summary>
    /// <param name="transfer"></param>
    /// <returns></returns>
    public static double? PercentComplete(this HttpTransfer transfer)
    {
        if (transfer.BytesToTransfer == null)
            return null;

        var result = Math.Round((double)transfer.BytesTransferred / transfer.BytesToTransfer.Value, 2);
        return result;
    }


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


    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    //public IObservable<int> WatchPendingTransfers() // downloads vs uploads
}
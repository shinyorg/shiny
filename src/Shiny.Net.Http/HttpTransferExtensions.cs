using System;
using System.IO;

namespace Shiny.Net.Http;


public static class HttpTransferExtensions
{
    /// <summary>
    /// Asserts that an HttpTransferRequest is valid
    /// </summary>
    public static void AssertValid(this HttpTransferRequest request)
    {
        if (request.Identifier.IsEmpty())
            throw new InvalidOperationException("Identifier is not set");

        if (request.Type != TransferType.Download)
        {
            if (!File.Exists(request.LocalFilePath))
                throw new ArgumentException($"{request.LocalFilePath} does not exist");
        }
    }


    /// <summary>
    /// Is the transfer type an upload?
    /// </summary>
    public static bool IsUpload(this TransferType type)
        => type != TransferType.Download;


    /// <summary>
    /// Monitors a specific transfer — completes when the transfer finishes, errors on failure.
    /// Unlike WhenUpdateReceived, this observable terminates.
    /// </summary>
    public static IObservable<HttpTransferResult> WatchTransfer(this IHttpTransferManager manager, string identifier)
        => new WatchTransferObservable(manager, identifier);
}

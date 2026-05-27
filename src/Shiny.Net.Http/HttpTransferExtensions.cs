using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


/// <summary>
/// Convenience extensions for working with HTTP transfers.
/// </summary>
public static class HttpTransferExtensions
{
    /// <summary>
    /// Throws if the given <see cref="HttpTransferRequest"/> is missing an identifier
    /// or, for uploads, the local file does not exist.
    /// </summary>
    /// <param name="request">The request to validate.</param>
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
    /// Returns true if the transfer type represents an upload (multipart or raw).
    /// </summary>
    /// <param name="type">The transfer type to test.</param>
    public static bool IsUpload(this TransferType type)
        => type != TransferType.Download;


    /// <summary>
    /// Waits for a specific transfer to reach a terminal state and returns the final result.
    /// </summary>
    /// <param name="manager">The transfer manager to subscribe to.</param>
    /// <param name="identifier">The identifier of the transfer to watch.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public static Task<HttpTransferResult> WatchTransfer(this IHttpTransferManager manager, string identifier, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<HttpTransferResult>();

        EventHandler<HttpTransferResult> handler = null!;
        handler = (_, result) =>
        {
            if (!result.Request.Identifier.Equals(identifier, StringComparison.InvariantCultureIgnoreCase))
                return;

            if (result.Exception != null)
            {
                manager.UpdateReceived -= handler;
                tcs.TrySetException(result.Exception);
            }
            else if (result.Status == HttpTransferState.Completed || result.Status == HttpTransferState.Canceled)
            {
                manager.UpdateReceived -= handler;
                tcs.TrySetResult(result);
            }
        };
        manager.UpdateReceived += handler;

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() =>
            {
                manager.UpdateReceived -= handler;
                tcs.TrySetCanceled(cancellationToken);
            });
        }

        return tcs.Task;
    }
}

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
    /// Waits for a specific transfer to complete or fail.
    /// The returned task completes when the transfer reaches Completed or Error state.
    /// </summary>
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
            cancellationToken.Register(() =>
            {
                manager.UpdateReceived -= handler;
                tcs.TrySetCanceled(cancellationToken);
            });

        return tcs.Task;
    }
}
